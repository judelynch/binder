using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using PokeBinder.Api.Dtos;
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
        Assert.Equal("Normal", result.Items[0].Variants[0].VariantTypeName);
    }

    private record TokenOnly(string Token);
}
