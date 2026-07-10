using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using PokeBinder.Api.Dtos;
using PokeBinder.Infrastructure;
using PokeBinder.Infrastructure.Cards.Import;
using Xunit;

namespace PokeBinder.Tests;

public class SetCardOrderingApiTests : IAsyncLifetime
{
    private static readonly string ConnectionString = CustomWebApplicationFactory.ConnectionStringFor("PokeBinderTest_Ordering");
    private static readonly string FixtureRoot = Path.Combine(AppContext.BaseDirectory, "Fixtures", "CardData");

    private CustomWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<PokeBinderDbContext>().UseSqlServer(ConnectionString).Options;
        await using (var db = new PokeBinderDbContext(options))
        {
            await db.Database.EnsureDeletedAsync();
            await db.Database.MigrateAsync();
            var importer = new CardDataImporter(db, new LocalDirectoryCardDataSource(FixtureRoot), NullLogger<CardDataImporter>.Instance);
            await importer.RunAsync();
        }

        _factory = new CustomWebApplicationFactory(ConnectionString);
        _client = _factory.CreateClient();

        var email = $"ordering-{Guid.NewGuid():N}@pokebinder.test";
        var register = await _client.PostAsJsonAsync("/api/auth/register", new { email, password = "OrderingTest123!" });
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
    public async Task GetSetCards_ReturnsNumericThenPrefixedGroupInOrder()
    {
        // test2 mixes plain numerics (1,2,2a,3) with a Radiant-Collection-style
        // prefixed group (RC1, RC2), shuffled in the source fixture file, the
        // same shape as the real "Legendary Treasures" (bw11) set.
        var response = await _client.GetAsync("/api/sets/test2/cards?pageSize=50");
        response.EnsureSuccessStatusCode();

        var page = await response.Content.ReadFromJsonAsync<PagedResult<CardSummaryDto>>();
        Assert.NotNull(page);
        Assert.Equal(6, page!.TotalCount);

        var orderedNumbers = page.Items.Select(c => c.Number).ToArray();
        Assert.Equal(new[] { "1", "2", "2a", "3", "RC1", "RC2" }, orderedNumbers);
    }

    private record TokenOnly(string Token);
}
