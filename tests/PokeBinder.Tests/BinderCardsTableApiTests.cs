using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using PokeBinder.Api.Dtos;
using PokeBinder.Infrastructure;
using Xunit;

namespace PokeBinder.Tests;

public class BinderCardsTableApiTests : IAsyncLifetime
{
    private static readonly string ConnectionString = CustomWebApplicationFactory.ConnectionStringFor("PokeBinderTest_BinderCardsTable");

    private CustomWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;
    private List<Guid> _variantIds = null!;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<PokeBinderDbContext>().UseSqlServer(ConnectionString).Options;
        await using (var db = new PokeBinderDbContext(options))
        {
            await db.Database.EnsureDeletedAsync();
            await db.Database.MigrateAsync();
            _variantIds = await CardFixture.SeedCardVariantsAsync(db, 3);
        }

        _factory = new CustomWebApplicationFactory(ConnectionString);
        _client = _factory.CreateClient();

        var email = $"cardstable-{Guid.NewGuid():N}@pokebinder.test";
        var register = await _client.PostAsJsonAsync("/api/auth/register", new { email, password = "CardsTable123!" });
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
    public async Task GetBinderCards_ReturnsFilledSlotsAcrossPagesInPageOrder_WithYearAndOwnedAndTag()
    {
        var createBinder = await _client.PostAsJsonAsync("/api/binders", new { name = "Table Binder", colourHex = "#336699", rows = 1, columns = 1, initialPageCount = 4 });
        createBinder.EnsureSuccessStatusCode();
        var binder = (await createBinder.Content.ReadFromJsonAsync<BinderSummaryDto>())!;

        var spread0 = (await (await _client.GetAsync($"/api/binders/{binder.Id}/spread/0")).Content.ReadFromJsonAsync<SpreadResponseDto>())!;
        var spread1 = (await (await _client.GetAsync($"/api/binders/{binder.Id}/spread/1")).Content.ReadFromJsonAsync<SpreadResponseDto>())!;
        var page1SlotId = spread0.RightPanel.Slots![0].SlotId;
        var page3SlotId = spread1.RightPanel.Slots![0].SlotId;

        // Assign page 3 first, then page 1, to prove the response is ordered by page number, not insertion order.
        (await _client.PutAsJsonAsync($"/api/binders/{binder.Id}/slots/{page3SlotId}", new { cardVariantId = _variantIds[1] })).EnsureSuccessStatusCode();
        (await _client.PutAsJsonAsync($"/api/binders/{binder.Id}/slots/{page1SlotId}", new { cardVariantId = _variantIds[0] })).EnsureSuccessStatusCode();

        var markOwned = await _client.PatchAsJsonAsync($"/api/binders/{binder.Id}/slots/{page1SlotId}", new { owned = true });
        markOwned.EnsureSuccessStatusCode();

        var response = await _client.GetAsync($"/api/binders/{binder.Id}/cards");
        response.EnsureSuccessStatusCode();
        var rows = (await response.Content.ReadFromJsonAsync<List<BinderCardRowDto>>())!;

        Assert.Equal(2, rows.Count);
        Assert.Equal(1, rows[0].PageNumber);
        Assert.Equal(3, rows[1].PageNumber);
        Assert.True(rows[0].Owned);
        Assert.False(rows[1].Owned);
        Assert.Equal(2020, rows[0].ReleaseYear); // CardFixture seeds ReleaseDate = 2020-01-01
        Assert.Null(rows[0].TagId);
    }

    [Fact]
    public async Task GetBinderCards_OnAnotherUsersBinder_ReturnsNotFound()
    {
        var createBinder = await _client.PostAsJsonAsync("/api/binders", new { name = "Private Binder", colourHex = "#336699", rows = 1, columns = 1, initialPageCount = 2 });
        var binder = (await createBinder.Content.ReadFromJsonAsync<BinderSummaryDto>())!;

        var otherClient = _factory.CreateClient();
        var email = $"other-{Guid.NewGuid():N}@pokebinder.test";
        var register = await otherClient.PostAsJsonAsync("/api/auth/register", new { email, password = "OtherUser123!" });
        var body = await register.Content.ReadFromJsonAsync<TokenOnly>();
        otherClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Token);

        var response = await otherClient.GetAsync($"/api/binders/{binder.Id}/cards");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private record TokenOnly(string Token);
}
