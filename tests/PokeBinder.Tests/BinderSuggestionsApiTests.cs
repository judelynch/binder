using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using PokeBinder.Api.Dtos;
using PokeBinder.Infrastructure;
using Xunit;

namespace PokeBinder.Tests;

public class BinderSuggestionsApiTests : IAsyncLifetime
{
    private static readonly string ConnectionString = CustomWebApplicationFactory.ConnectionStringFor("PokeBinderTest_Suggestions");

    private CustomWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;
    private Dictionary<string, Guid> _variantIds = null!;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<PokeBinderDbContext>().UseSqlServer(ConnectionString).Options;
        await using (var db = new PokeBinderDbContext(options))
        {
            await db.Database.EnsureDeletedAsync();
            await db.Database.MigrateAsync();
            _variantIds = await SuggestionCardFixture.SeedAsync(db);
        }

        _factory = new CustomWebApplicationFactory(ConnectionString);
        _client = _factory.CreateClient();

        var email = $"suggest-{Guid.NewGuid():N}@pokebinder.test";
        var register = await _client.PostAsJsonAsync("/api/auth/register", new { email, password = "SuggestTest123!" });
        register.EnsureSuccessStatusCode();
        var body = await register.Content.ReadFromJsonAsync<TokenOnly>();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Token);
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    private async Task<(Guid BinderId, IReadOnlyList<Guid> SlotIds)> CreateBinderWithSlotsAsync(int count = 4)
    {
        var createBinder = await _client.PostAsJsonAsync("/api/binders", new { name = "Suggestion Binder", colourHex = "#336699", rows = 3, columns = 3, initialPageCount = 2 });
        createBinder.EnsureSuccessStatusCode();
        var binder = (await createBinder.Content.ReadFromJsonAsync<BinderSummaryDto>())!;

        var spreadResponse = await _client.GetAsync($"/api/binders/{binder.Id}/spread/0");
        spreadResponse.EnsureSuccessStatusCode();
        var spread = (await spreadResponse.Content.ReadFromJsonAsync<SpreadResponseDto>())!;
        return (binder.Id, spread.RightPanel.Slots!.Take(count).Select(s => s.SlotId).ToList());
    }

    private async Task AssignAsync(Guid binderId, Guid slotId, string cardId) =>
        (await _client.PutAsJsonAsync($"/api/binders/{binderId}/slots/{slotId}", new { cardVariantId = _variantIds[cardId] })).EnsureSuccessStatusCode();

    private async Task<List<SlotSuggestionsDto>> GetSuggestionsAsync(Guid binderId)
    {
        var response = await _client.GetAsync($"/api/binders/{binderId}/spread/0/suggestions");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<List<SlotSuggestionsDto>>())!;
    }

    [Fact]
    public async Task SetTheme_TwoCardsFromTheSameSet_SuggestsTheGapBetweenThemFromBothSides()
    {
        // Gengar (#1) and Oddish (#3) share a set and nothing else, so the Set theme wins; the
        // missing Haunter (#2) between them should be suggested from both directions.
        var (binderId, slots) = await CreateBinderWithSlotsAsync();
        await AssignAsync(binderId, slots[0], SuggestionCardFixture.GengarSet1);
        await AssignAsync(binderId, slots[1], SuggestionCardFixture.OddishSet1);

        var suggestions = await GetSuggestionsAsync(binderId);

        var gengarSuggestions = suggestions.Single(s => s.SlotId == slots[0]).Suggestions;
        var gengarSuggestion = Assert.Single(gengarSuggestions);
        Assert.Equal(SuggestionCardFixture.HaunterSet1, gengarSuggestion.CardId);
        Assert.Equal("NextInSet", gengarSuggestion.Reason);

        var oddishSuggestions = suggestions.Single(s => s.SlotId == slots[1]).Suggestions;
        var oddishSuggestion = Assert.Single(oddishSuggestions);
        Assert.Equal(SuggestionCardFixture.HaunterSet1, oddishSuggestion.CardId);
        Assert.Equal("PrevInSet", oddishSuggestion.Reason);
        Assert.Equal("Suggestion Set One", oddishSuggestion.SetName);
        Assert.NotEqual(Guid.Empty, oddishSuggestion.CardVariantId);
    }

    [Fact]
    public async Task NameTheme_TwoGengarsFromDifferentSets_SuggestsTheNextPrintForBoth()
    {
        // Gengar (set1) and Gengar (set2) share neither a set nor a rarity+type/rarity+supertype
        // group as large as their shared name, so the Name theme wins.
        var (binderId, slots) = await CreateBinderWithSlotsAsync();
        await AssignAsync(binderId, slots[0], SuggestionCardFixture.GengarSet1);
        await AssignAsync(binderId, slots[1], SuggestionCardFixture.GengarSet2);

        var suggestions = await GetSuggestionsAsync(binderId);

        foreach (var slotId in new[] { slots[0], slots[1] })
        {
            var slotSuggestions = suggestions.Single(s => s.SlotId == slotId).Suggestions;
            var suggestion = Assert.Single(slotSuggestions);
            Assert.Equal(SuggestionCardFixture.GengarSet3, suggestion.CardId);
            Assert.Equal("NextRelease", suggestion.Reason);
        }
    }

    [Fact]
    public async Task RaritySupertypeTheme_TwoUltraRareTrainers_SuggestsAnotherOne()
    {
        // Bill and Oak are both Ultra Rare Trainers in their own separate sets, with no shared name
        // and no element type at all (Trainers aren't Pokémon) - RaritySupertype is the only
        // category with a group bigger than 1, so it wins.
        var (binderId, slots) = await CreateBinderWithSlotsAsync();
        await AssignAsync(binderId, slots[0], SuggestionCardFixture.TrainerBill);
        await AssignAsync(binderId, slots[1], SuggestionCardFixture.TrainerOak);

        var suggestions = await GetSuggestionsAsync(binderId);

        foreach (var slotId in new[] { slots[0], slots[1] })
        {
            var slotSuggestions = suggestions.Single(s => s.SlotId == slotId).Suggestions;
            var suggestion = Assert.Single(slotSuggestions);
            Assert.Equal(SuggestionCardFixture.TrainerMarnie, suggestion.CardId);
            Assert.Equal("SameThemeRarity", suggestion.Reason);
        }
    }

    [Fact]
    public async Task GetSuggestions_EmptyBinder_ReturnsNoSuggestions()
    {
        var (binderId, _) = await CreateBinderWithSlotsAsync();

        var suggestions = await GetSuggestionsAsync(binderId);

        Assert.Empty(suggestions);
    }

    private record TokenOnly(string Token);
}
