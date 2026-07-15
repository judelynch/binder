using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using PokeBinder.Api.Dtos;
using PokeBinder.Infrastructure;
using Xunit;

namespace PokeBinder.Tests;

public class SetCompletionTests : IAsyncLifetime
{
    private static readonly string ConnectionString = CustomWebApplicationFactory.ConnectionStringFor("PokeBinderTest_SetCompletion");

    private CustomWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;
    private SetCompletionFixtureIds _ids = null!;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<PokeBinderDbContext>().UseSqlServer(ConnectionString).Options;
        await using (var db = new PokeBinderDbContext(options))
        {
            await db.Database.EnsureDeletedAsync();
            await db.Database.MigrateAsync();
            _ids = await SetCompletionFixture.SeedAsync(db);
        }

        _factory = new CustomWebApplicationFactory(ConnectionString);
        _client = _factory.CreateClient();

        var email = $"completion-{Guid.NewGuid():N}@pokebinder.test";
        var register = await _client.PostAsJsonAsync("/api/auth/register", new { email, password = "CompletionTest123!" });
        register.EnsureSuccessStatusCode();
        var body = await register.Content.ReadFromJsonAsync<TokenOnly>();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Token);
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    private async Task<SetSummaryDto> GetCompletionSetAsync()
    {
        var response = await _client.GetAsync("/api/sets");
        response.EnsureSuccessStatusCode();
        var sets = await response.Content.ReadFromJsonAsync<List<SetSummaryDto>>();
        return sets!.Single(s => s.Id == _ids.SetId);
    }

    [Fact]
    public async Task NoOwnership_OnlyVacuouslyCompleteCards_Count()
    {
        // Card 2 (Promo Stamp only) and Card 4 (STAMP only) have zero required variants, so both
        // count as complete even with no ownership at all. Card 1 and Card 3 have real required
        // variants and are not owned, so they don't count.
        var set = await GetCompletionSetAsync();

        Assert.Equal(4, set.CardCount);
        Assert.Equal(2, set.OwnedCount);
    }

    [Fact]
    public async Task OwningRequiredVariants_ButNotStamp_CompletesTheCard()
    {
        var putNormal = await _client.PutAsJsonAsync(
            $"/api/collection/ownership/{_ids.Card1NormalVariantId}", new { quantity = 1, condition = (string?)null });
        putNormal.EnsureSuccessStatusCode();

        var putReverseHolo = await _client.PutAsJsonAsync(
            $"/api/collection/ownership/{_ids.Card1ReverseHoloVariantId}", new { quantity = 1, condition = (string?)null });
        putReverseHolo.EnsureSuccessStatusCode();

        // Deliberately never own Card 1's Promo Stamp variant.
        var set = await GetCompletionSetAsync();

        Assert.Equal(4, set.CardCount);
        Assert.Equal(3, set.OwnedCount); // Card 1 (now complete) + Card 2 + Card 4 (vacuous)
    }

    [Fact]
    public async Task GetSetCards_ReflectsPerVariantOwnership_AndLeavesStampUnowned()
    {
        await _client.PutAsJsonAsync($"/api/collection/ownership/{_ids.Card1NormalVariantId}", new { quantity = 1, condition = (string?)null });
        await _client.PutAsJsonAsync($"/api/collection/ownership/{_ids.Card1ReverseHoloVariantId}", new { quantity = 2, condition = "NM" });

        var response = await _client.GetAsync($"/api/sets/{_ids.SetId}/cards?pageSize=10");
        response.EnsureSuccessStatusCode();
        var page = await response.Content.ReadFromJsonAsync<PagedResult<CardSummaryDto>>();
        var card1 = page!.Items.Single(c => c.Id == "completion-1");

        var normalVariant = card1.Variants.Single(v => v.Id == _ids.Card1NormalVariantId);
        var reverseHoloVariant = card1.Variants.Single(v => v.Id == _ids.Card1ReverseHoloVariantId);
        var stampVariant = card1.Variants.Single(v => v.Id == _ids.Card1PromoStampVariantId);

        Assert.True(normalVariant.Owned);
        Assert.Equal(1, normalVariant.Quantity);
        Assert.True(reverseHoloVariant.Owned);
        Assert.Equal(2, reverseHoloVariant.Quantity);
        Assert.Equal("NM", reverseHoloVariant.Condition);
        Assert.False(stampVariant.Owned);
    }

    private record TokenOnly(string Token);
}
