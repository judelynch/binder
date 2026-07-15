using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using PokeBinder.Api.Dtos;
using PokeBinder.Core.Cards;
using PokeBinder.Infrastructure;
using Xunit;

namespace PokeBinder.Tests;

public class CollectionApiTests : IAsyncLifetime
{
    private static readonly string ConnectionString = CustomWebApplicationFactory.ConnectionStringFor("PokeBinderTest_Collection");

    private CustomWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;
    private Guid _variantId;
    private Guid _variantId2;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<PokeBinderDbContext>().UseSqlServer(ConnectionString).Options;
        await using (var db = new PokeBinderDbContext(options))
        {
            await db.Database.EnsureDeletedAsync();
            await db.Database.MigrateAsync();

            var set = new Set
            {
                Id = "collection-set",
                Name = "Collection Test Set",
                Series = "Collection Series",
                PrintedTotal = 1,
                Total = 1,
                ReleaseDate = new DateOnly(2022, 1, 1),
                UpdatedAt = DateTime.UtcNow,
            };
            db.Sets.Add(set);

            var variantType = new VariantType { Id = Guid.NewGuid(), Name = "Normal" };
            db.VariantTypes.Add(variantType);

            var card = new Card { Id = "collection-1", SetId = set.Id, Name = "Squirtle", Supertype = "Pokémon", Number = "1" };
            var card2 = new Card { Id = "collection-2", SetId = set.Id, Name = "Wartortle", Supertype = "Pokémon", Number = "2" };
            db.Cards.AddRange(card, card2);

            _variantId = Guid.NewGuid();
            _variantId2 = Guid.NewGuid();
            db.CardVariants.Add(new CardVariant { Id = _variantId, CardId = card.Id, VariantTypeId = variantType.Id });
            db.CardVariants.Add(new CardVariant { Id = _variantId2, CardId = card2.Id, VariantTypeId = variantType.Id });

            await db.SaveChangesAsync();
        }

        _factory = new CustomWebApplicationFactory(ConnectionString);
        _client = _factory.CreateClient();
        await AuthenticateAsync(_client, "collection-a");
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    private async Task AuthenticateAsync(HttpClient client, string emailPrefix)
    {
        var email = $"{emailPrefix}-{Guid.NewGuid():N}@pokebinder.test";
        var register = await client.PostAsJsonAsync("/api/auth/register", new { email, password = "CollectionTest123!" });
        register.EnsureSuccessStatusCode();
        var body = await register.Content.ReadFromJsonAsync<TokenOnly>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Token);
    }

    [Fact]
    public async Task Put_CreatesOwnership()
    {
        var response = await _client.PutAsJsonAsync($"/api/collection/ownership/{_variantId}", new { quantity = 1, condition = (string?)null });
        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<CardOwnershipDto>();

        Assert.True(dto!.Owned);
        Assert.Equal(1, dto.Quantity);
    }

    [Fact]
    public async Task Put_Twice_UpdatesInPlace_NoDuplicateRow()
    {
        await _client.PutAsJsonAsync($"/api/collection/ownership/{_variantId}", new { quantity = 1, condition = (string?)null });
        var second = await _client.PutAsJsonAsync($"/api/collection/ownership/{_variantId}", new { quantity = 3, condition = "NM" });
        second.EnsureSuccessStatusCode();
        var dto = await second.Content.ReadFromJsonAsync<CardOwnershipDto>();

        Assert.Equal(3, dto!.Quantity);
        Assert.Equal("NM", dto.Condition);

        var options = new DbContextOptionsBuilder<PokeBinderDbContext>().UseSqlServer(ConnectionString).Options;
        await using var db = new PokeBinderDbContext(options);
        var rowCount = await db.CardOwnerships.CountAsync(o => o.CardVariantId == _variantId);
        Assert.Equal(1, rowCount);
    }

    [Fact]
    public async Task Put_QuantityLessThanOne_ReturnsBadRequest()
    {
        var response = await _client.PutAsJsonAsync($"/api/collection/ownership/{_variantId}", new { quantity = 0, condition = (string?)null });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Put_UnknownVariant_ReturnsNotFound()
    {
        var response = await _client.PutAsJsonAsync($"/api/collection/ownership/{Guid.NewGuid()}", new { quantity = 1, condition = (string?)null });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_RemovesOwnership_AndIsIdempotent()
    {
        await _client.PutAsJsonAsync($"/api/collection/ownership/{_variantId}", new { quantity = 1, condition = (string?)null });

        var first = await _client.DeleteAsync($"/api/collection/ownership/{_variantId}");
        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);

        var second = await _client.DeleteAsync($"/api/collection/ownership/{_variantId}");
        Assert.Equal(HttpStatusCode.NoContent, second.StatusCode);
    }

    [Fact]
    public async Task Ownership_IsScopedPerUser()
    {
        await _client.PutAsJsonAsync($"/api/collection/ownership/{_variantId}", new { quantity = 1, condition = (string?)null });

        using var otherClient = _factory.CreateClient();
        await AuthenticateAsync(otherClient, "collection-b");
        var otherResponse = await otherClient.PutAsJsonAsync($"/api/collection/ownership/{_variantId}", new { quantity = 5, condition = (string?)null });
        otherResponse.EnsureSuccessStatusCode();

        // Same variant, two different users -> two independent rows (proves the unique index is
        // keyed on (UserId, CardVariantId), not CardVariantId alone) with each user's own quantity.
        var options = new DbContextOptionsBuilder<PokeBinderDbContext>().UseSqlServer(ConnectionString).Options;
        await using var db = new PokeBinderDbContext(options);
        var ownershipRows = await db.CardOwnerships.Where(o => o.CardVariantId == _variantId).ToListAsync();
        Assert.Equal(2, ownershipRows.Count);
        Assert.Contains(ownershipRows, o => o.Quantity == 1);
        Assert.Contains(ownershipRows, o => o.Quantity == 5);
    }

    [Fact]
    public async Task Bulk_MarksMultipleVariantsOwned_LeavingAlreadyOwnedUntouched()
    {
        // Pre-own variant 1 with a specific quantity/condition; bulk-mark both 1 and 2 as owned.
        await _client.PutAsJsonAsync($"/api/collection/ownership/{_variantId}", new { quantity = 4, condition = "LP" });

        var response = await _client.PostAsJsonAsync(
            "/api/collection/ownership/bulk", new { cardVariantIds = new[] { _variantId, _variantId2 }, owned = true });
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<BulkOwnershipResultDto>();

        // Only variant 2 was newly created - variant 1 was already owned and must be left as-is.
        Assert.Equal(1, result!.Count);

        var options = new DbContextOptionsBuilder<PokeBinderDbContext>().UseSqlServer(ConnectionString).Options;
        await using var db = new PokeBinderDbContext(options);
        var ownership1 = await db.CardOwnerships.SingleAsync(o => o.CardVariantId == _variantId);
        var ownership2 = await db.CardOwnerships.SingleAsync(o => o.CardVariantId == _variantId2);
        Assert.Equal(4, ownership1.Quantity); // untouched
        Assert.Equal(1, ownership2.Quantity); // newly created, default quantity
    }

    [Fact]
    public async Task Bulk_UnmarksMultipleVariants()
    {
        await _client.PutAsJsonAsync($"/api/collection/ownership/{_variantId}", new { quantity = 1, condition = (string?)null });
        await _client.PutAsJsonAsync($"/api/collection/ownership/{_variantId2}", new { quantity = 1, condition = (string?)null });

        var response = await _client.PostAsJsonAsync(
            "/api/collection/ownership/bulk", new { cardVariantIds = new[] { _variantId, _variantId2 }, owned = false });
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<BulkOwnershipResultDto>();
        Assert.Equal(2, result!.Count);

        var options = new DbContextOptionsBuilder<PokeBinderDbContext>().UseSqlServer(ConnectionString).Options;
        await using var db = new PokeBinderDbContext(options);
        var remaining = await db.CardOwnerships.CountAsync(o => o.CardVariantId == _variantId || o.CardVariantId == _variantId2);
        Assert.Equal(0, remaining);
    }

    [Fact]
    public async Task Bulk_UnknownVariant_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/collection/ownership/bulk", new { cardVariantIds = new[] { _variantId, Guid.NewGuid() }, owned = true });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private record TokenOnly(string Token);
}
