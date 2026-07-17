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

    [Fact]
    public async Task GetSuggestions_ReturnsNextInSetNextReleaseAndThemeSuggestions()
    {
        var createBinder = await _client.PostAsJsonAsync("/api/binders", new { name = "Suggestion Binder", colourHex = "#336699", rows = 3, columns = 3, initialPageCount = 2 });
        createBinder.EnsureSuccessStatusCode();
        var binder = (await createBinder.Content.ReadFromJsonAsync<BinderSummaryDto>())!;

        var spreadResponse = await _client.GetAsync($"/api/binders/{binder.Id}/spread/0");
        spreadResponse.EnsureSuccessStatusCode();
        var spread = (await spreadResponse.Content.ReadFromJsonAsync<SpreadResponseDto>())!;
        var slots = spread.RightPanel.Slots!;

        // Place Gengar (set1) and Oddish (set1, Illustration Rare/Grass) into the first two slots.
        var gengarSlotId = slots[0].SlotId;
        var oddishSlotId = slots[1].SlotId;

        var assignGengar = await _client.PutAsJsonAsync($"/api/binders/{binder.Id}/slots/{gengarSlotId}", new { cardVariantId = _variantIds[SuggestionCardFixture.GengarSet1] });
        assignGengar.EnsureSuccessStatusCode();
        var assignOddish = await _client.PutAsJsonAsync($"/api/binders/{binder.Id}/slots/{oddishSlotId}", new { cardVariantId = _variantIds[SuggestionCardFixture.OddishSet1] });
        assignOddish.EnsureSuccessStatusCode();

        var suggestionsResponse = await _client.GetAsync($"/api/binders/{binder.Id}/spread/0/suggestions");
        suggestionsResponse.EnsureSuccessStatusCode();
        var suggestions = (await suggestionsResponse.Content.ReadFromJsonAsync<List<SlotSuggestionsDto>>())!;

        var gengarSuggestions = suggestions.Single(s => s.SlotId == gengarSlotId).Suggestions;
        Assert.Equal(2, gengarSuggestions.Count);
        Assert.Contains(gengarSuggestions, s => s.CardId == SuggestionCardFixture.HaunterSet1 && s.Reason == "NextInSet");
        Assert.Contains(gengarSuggestions, s => s.CardId == SuggestionCardFixture.GengarSet2 && s.Reason == "NextRelease");

        var oddishSuggestions = suggestions.Single(s => s.SlotId == oddishSlotId).Suggestions;
        var themeSuggestion = Assert.Single(oddishSuggestions);
        Assert.Equal(SuggestionCardFixture.TangelaSet2, themeSuggestion.CardId);
        Assert.Equal("SameThemeRarity", themeSuggestion.Reason);
        Assert.Equal("Suggestion Set Two", themeSuggestion.SetName);
        Assert.NotEqual(Guid.Empty, themeSuggestion.CardVariantId);
    }

    [Fact]
    public async Task GetSuggestions_EmptyBinder_ReturnsNoSuggestions()
    {
        var createBinder = await _client.PostAsJsonAsync("/api/binders", new { name = "Empty Binder", colourHex = "#336699", rows = 3, columns = 3, initialPageCount = 2 });
        createBinder.EnsureSuccessStatusCode();
        var binder = (await createBinder.Content.ReadFromJsonAsync<BinderSummaryDto>())!;

        var response = await _client.GetAsync($"/api/binders/{binder.Id}/spread/0/suggestions");
        response.EnsureSuccessStatusCode();
        var suggestions = await response.Content.ReadFromJsonAsync<List<SlotSuggestionsDto>>();

        Assert.Empty(suggestions!);
    }

    private record TokenOnly(string Token);
}
