using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using PokeBinder.Api.Dtos;
using PokeBinder.Infrastructure;
using Xunit;

namespace PokeBinder.Tests;

public class BulkAssignOrderingAndDryRunTests : IAsyncLifetime
{
    private static readonly string ConnectionString = CustomWebApplicationFactory.ConnectionStringFor("PokeBinderTest_BulkOrder");

    private CustomWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;
    private List<Guid> _cardVariantIds = null!;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<PokeBinderDbContext>().UseSqlServer(ConnectionString).Options;
        await using (var db = new PokeBinderDbContext(options))
        {
            await db.Database.EnsureDeletedAsync();
            await db.Database.MigrateAsync();
            _cardVariantIds = await CardFixture.SeedCardVariantsAsync(db, 6);
        }

        _factory = new CustomWebApplicationFactory(ConnectionString);
        _client = _factory.CreateClient();

        var email = $"bulkorder-{Guid.NewGuid():N}@pokebinder.test";
        var register = await _client.PostAsJsonAsync("/api/auth/register", new { email, password = "BulkOrder123!" });
        register.EnsureSuccessStatusCode();
        var body = await register.Content.ReadFromJsonAsync<TokenOnly>();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Token);
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    private async Task<(Guid BinderId, Guid StartSlotId)> CreateBinderAsync(int rows, int columns, int pages)
    {
        var response = await _client.PostAsJsonAsync("/api/binders", new
        {
            name = "Order Test",
            colourHex = "#3E7CB1",
            rows,
            columns,
            initialPageCount = pages
        });
        response.EnsureSuccessStatusCode();
        var binder = (await response.Content.ReadFromJsonAsync<BinderSummaryDto>())!;

        var spreadResponse = await _client.GetAsync($"/api/binders/{binder.Id}/spread/0");
        spreadResponse.EnsureSuccessStatusCode();
        var spread = (await spreadResponse.Content.ReadFromJsonAsync<SpreadResponseDto>())!;

        return (binder.Id, spread.RightPanel.Slots![0].SlotId);
    }

    [Fact]
    public async Task BulkAssign_PlacesCardsInGivenOrder()
    {
        var (binderId, startSlotId) = await CreateBinderAsync(rows: 1, columns: 6, pages: 2);

        // Deliberately reversed vs. seed order to prove placement follows the request, not some other ordering.
        var orderedIds = new[] { _cardVariantIds[3], _cardVariantIds[0], _cardVariantIds[5] };

        var response = await _client.PostAsJsonAsync($"/api/binders/{binderId}/slots/bulk-assign", new
        {
            cardVariantIds = orderedIds,
            startSlotId,
            occupiedStrategy = "overwrite"
        });
        response.EnsureSuccessStatusCode();

        var spreadResponse = await _client.GetAsync($"/api/binders/{binderId}/spread/0");
        var spread = (await spreadResponse.Content.ReadFromJsonAsync<SpreadResponseDto>())!;
        var placedCardIds = spread.RightPanel.Slots!.Take(3).Select(s => s.Card!.Id).ToList();

        Assert.Equal(new[] { "fixture-set-4", "fixture-set-1", "fixture-set-6" }, placedCardIds);
    }

    [Fact]
    public async Task DryRun_ReportsAccurateCountsWithoutPersisting()
    {
        var (binderId, startSlotId) = await CreateBinderAsync(rows: 1, columns: 1, pages: 2); // 2 slots only

        var response = await _client.PostAsJsonAsync($"/api/binders/{binderId}/slots/bulk-assign?dryRun=true", new
        {
            cardVariantIds = _cardVariantIds.Take(5).ToArray(), // needs 5 slots, only 2 exist
            startSlotId,
            occupiedStrategy = "overwrite"
        });
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<BulkAssignResultDto>();

        Assert.Equal(5, result!.Placed);
        Assert.True(result.PagesAdded > 0);

        // Nothing should actually have been written: binder still has its original 2 slots, all empty.
        var binderResponse = await _client.GetAsync($"/api/binders/{binderId}");
        var binder = (await binderResponse.Content.ReadFromJsonAsync<BinderSummaryDto>())!;
        Assert.Equal(2, binder.PageCount);
        Assert.Equal(0, binder.FilledSlots);
    }

    private record TokenOnly(string Token);
}
