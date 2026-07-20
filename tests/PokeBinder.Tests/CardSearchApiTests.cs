using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using PokeBinder.Api.Dtos;
using PokeBinder.Core.Pricing;
using PokeBinder.Infrastructure;
using Xunit;

namespace PokeBinder.Tests;

public class CardSearchApiTests : IAsyncLifetime
{
    private static readonly string ConnectionString = CustomWebApplicationFactory.ConnectionStringFor("PokeBinderTest_Search");

    private CustomWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<PokeBinderDbContext>().UseSqlServer(ConnectionString).Options;
        await using (var db = new PokeBinderDbContext(options))
        {
            await db.Database.EnsureDeletedAsync();
            await db.Database.MigrateAsync();
            await SearchCardFixture.SeedAsync(db);
        }

        _factory = new CustomWebApplicationFactory(ConnectionString);
        _client = _factory.CreateClient();

        var email = $"search-{Guid.NewGuid():N}@pokebinder.test";
        var register = await _client.PostAsJsonAsync("/api/auth/register", new { email, password = "SearchTest123!" });
        register.EnsureSuccessStatusCode();
        var body = await register.Content.ReadFromJsonAsync<TokenOnly>();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Token);
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    private async Task<PagedResult<CardSearchResultDto>> SearchAsync(string query)
    {
        var response = await _client.GetAsync($"/api/cards/search?{query}");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PagedResult<CardSearchResultDto>>())!;
    }

    [Fact]
    public async Task Name_FiltersByContains()
    {
        var result = await SearchAsync("name=char");
        Assert.Single(result.Items);
        Assert.Equal("search-2", result.Items[0].Id);
    }

    [Fact]
    public async Task Supertype_FiltersExactly()
    {
        var result = await SearchAsync("supertype=Trainer");
        Assert.Single(result.Items);
        Assert.Equal("search-4", result.Items[0].Id);
    }

    [Fact]
    public async Task Types_MultiValue_MatchesAny()
    {
        var result = await SearchAsync("types=Fire&types=Water");
        var ids = result.Items.Select(i => i.Id).ToHashSet();
        Assert.Equal(new HashSet<string> { "search-2", "search-3" }, ids);
    }

    [Fact]
    public async Task Subtypes_FiltersByStage2()
    {
        var result = await SearchAsync("subtypes=Stage+2");
        Assert.Equal(2, result.TotalCount);
    }

    [Fact]
    public async Task HpRange_FiltersInclusively()
    {
        var result = await SearchAsync("hpMin=100&hpMax=200");
        var ids = result.Items.Select(i => i.Id).ToHashSet();
        Assert.Equal(new HashSet<string> { "search-2", "search-3" }, ids);
    }

    [Fact]
    public async Task WeaknessType_FiltersCorrectly()
    {
        var result = await SearchAsync("weaknessType=Water");
        Assert.Single(result.Items);
        Assert.Equal("search-2", result.Items[0].Id); // Charizard is weak to Water
    }

    [Fact]
    public async Task Artist_FiltersByContains()
    {
        var result = await SearchAsync("artist=Sugimori");
        var ids = result.Items.Select(i => i.Id).ToHashSet();
        Assert.Equal(new HashSet<string> { "search-3", "search-4" }, ids);
    }

    [Fact]
    public async Task CombinedFilters_NarrowCorrectly()
    {
        // Stage 2 + Rare Holo + HP >= 180 -> both Charizard and Blastoise; add type=Fire -> just Charizard.
        var result = await SearchAsync("subtypes=Stage+2&rarities=Rare+Holo&hpMin=180&types=Fire");
        Assert.Single(result.Items);
        Assert.Equal("search-2", result.Items[0].Id);
    }

    [Fact]
    public async Task Sort_Name_OrdersAlphabetically()
    {
        var result = await SearchAsync("sort=name&pageSize=10");
        var names = result.Items.Select(i => i.Name).ToList();
        Assert.Equal(names.OrderBy(n => n, StringComparer.Ordinal), names);
    }

    [Fact]
    public async Task Sort_NameDescending_ReversesOrder()
    {
        var result = await SearchAsync("sort=name&sortDescending=true&pageSize=10");
        var names = result.Items.Select(i => i.Name).ToList();
        Assert.Equal(names.OrderByDescending(n => n, StringComparer.Ordinal), names);
    }

    [Fact]
    public async Task Sort_RarityAscending_PutsCommonsFirst()
    {
        // Default rarity sort is rarest-first; explicit ascending should flip to commons-first.
        var result = await SearchAsync("sort=rarity&sortDescending=false&pageSize=10");
        Assert.Equal("Common", result.Items[0].Rarity);
    }

    [Fact]
    public async Task VariantType_FiltersToCardsWithThatVariant()
    {
        // Only Charizard (search-2) also comes in Reverse Holo per SearchCardFixture.
        var result = await SearchAsync("variantTypes=Reverse+Holo");
        Assert.Single(result.Items);
        Assert.Equal("search-2", result.Items[0].Id);
    }

    [Fact]
    public async Task VariantType_Normal_MatchesEveryCard()
    {
        var result = await SearchAsync("variantTypes=Normal");
        Assert.Equal(5, result.TotalCount);
    }

    [Fact]
    public async Task GetVariantTypeNames_ReturnsAllSeededNames()
    {
        var response = await _client.GetAsync("/api/cards/variant-types");
        response.EnsureSuccessStatusCode();
        var names = await response.Content.ReadFromJsonAsync<List<string>>();
        Assert.Equal(new[] { "Normal", "Reverse Holo" }, names);
    }

    [Fact]
    public async Task Pagination_ReturnsCorrectSlice()
    {
        var page1 = await SearchAsync("sort=name&page=1&pageSize=2");
        var page2 = await SearchAsync("sort=name&page=2&pageSize=2");

        Assert.Equal(2, page1.Items.Count);
        Assert.Equal(5, page1.TotalCount);
        Assert.Empty(page1.Items.Select(i => i.Id).Intersect(page2.Items.Select(i => i.Id)));
    }

    [Fact]
    public async Task Results_IncludeVariants()
    {
        var result = await SearchAsync("name=char");
        Assert.NotEmpty(result.Items[0].Variants);
        Assert.Contains(result.Items[0].Variants, v => v.VariantTypeName == "Normal");
    }

    [Fact]
    public async Task GetCard_ReturnsFullStats()
    {
        var response = await _client.GetAsync("/api/cards/search-2");
        response.EnsureSuccessStatusCode();
        var card = await response.Content.ReadFromJsonAsync<CardDetailDto>();

        Assert.NotNull(card);
        Assert.Equal("Charizard", card!.Name);
        Assert.Single(card.Abilities);
        Assert.Equal("Energy Burn", card.Abilities[0].Name);
        Assert.Single(card.Attacks);
        Assert.Equal("Fire Spin", card.Attacks[0].Name);
        Assert.Single(card.Weaknesses);
        Assert.Equal("Water", card.Weaknesses[0].Type);
        Assert.Empty(card.Resistances);
        Assert.Equal(new[] { "Colorless", "Colorless", "Colorless" }, card.RetreatCost);
        Assert.Equal(3, card.ConvertedRetreatCost);
    }

    [Fact]
    public async Task Results_OrderVariantsWithNormalFirst()
    {
        // Charizard (search-2) has both a Normal and a Reverse Holo variant. Without an explicit sort,
        // SQL Server gives no ordering guarantee at all (insertion order is not preserved) — this
        // must hold on the strength of the query's own ORDER BY, not incidentally.
        var result = await SearchAsync("name=char");
        var variantNames = result.Items[0].Variants.Select(v => v.VariantTypeName).ToList();
        Assert.Equal("Normal", variantNames[0]);
    }

    private async Task SeedPriceAsync(string cardId, decimal itemOnlyGbp)
    {
        var options = new DbContextOptionsBuilder<PokeBinderDbContext>().UseSqlServer(ConnectionString).Options;
        await using var db = new PokeBinderDbContext(options);
        var variantId = await db.CardVariants.Where(v => v.CardId == cardId).Select(v => v.Id).FirstAsync();
        db.PricePoints.Add(new PricePoint
        {
            Id = Guid.NewGuid(),
            CardVariantId = variantId,
            GradedStatus = GradedStatus.Raw,
            Condition = RawConditionClassification.NM,
            WindowDays = 30,
            ItemOnlyMedianGbp = itemOnlyGbp,
            DeliveredMedianGbp = itemOnlyGbp + 1,
            SampleCount = 3,
            MinGbp = itemOnlyGbp,
            MaxGbp = itemOnlyGbp,
            LastSaleDate = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task PriceRange_FiltersByBestAvailablePrice()
    {
        await SeedPriceAsync("search-1", 5.00m); // Pikachu - in range
        await SeedPriceAsync("search-2", 500.00m); // Charizard - out of range

        var result = await SearchAsync("priceMin=1&priceMax=10");

        Assert.Single(result.Items);
        Assert.Equal("search-1", result.Items[0].Id);
    }

    [Fact]
    public async Task HasPriceData_ExcludesCardsWithNoPricePoints()
    {
        await SeedPriceAsync("search-1", 5.00m); // only Pikachu is priced

        var result = await SearchAsync("hasPriceData=true");

        Assert.Single(result.Items);
        Assert.Equal("search-1", result.Items[0].Id);
    }

    private record TokenOnly(string Token);
}
