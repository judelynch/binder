using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using PokeBinder.Api.Dtos;
using PokeBinder.Infrastructure;
using Xunit;

namespace PokeBinder.Tests;

public class BulkSlotOperationsApiTests : IAsyncLifetime
{
    private static readonly string ConnectionString = CustomWebApplicationFactory.ConnectionStringFor("PokeBinderTest_BulkSlots");

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
            _variantIds = await CardFixture.SeedCardVariantsAsync(db, 4);
        }

        _factory = new CustomWebApplicationFactory(ConnectionString);
        _client = _factory.CreateClient();

        var email = $"bulkslots-{Guid.NewGuid():N}@pokebinder.test";
        var register = await _client.PostAsJsonAsync("/api/auth/register", new { email, password = "BulkSlots123!" });
        register.EnsureSuccessStatusCode();
        var body = await register.Content.ReadFromJsonAsync<TokenOnly>();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Token);
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    /// <summary>1 slot per page, 4 pages -> spread 0 = cover+page1, spread 1 = page2+page3, spread 2 = page4+cover.</summary>
    private async Task<(Guid BinderId, Guid Page1Slot, Guid Page3Slot)> CreateBinderWithTwoAssignedSlotsAcrossSpreadsAsync()
    {
        var createBinder = await _client.PostAsJsonAsync("/api/binders", new { name = "Bulk Ops Binder", colourHex = "#336699", rows = 1, columns = 1, initialPageCount = 4 });
        createBinder.EnsureSuccessStatusCode();
        var binder = (await createBinder.Content.ReadFromJsonAsync<BinderSummaryDto>())!;

        var spread0 = (await (await _client.GetAsync($"/api/binders/{binder.Id}/spread/0")).Content.ReadFromJsonAsync<SpreadResponseDto>())!;
        var spread1 = (await (await _client.GetAsync($"/api/binders/{binder.Id}/spread/1")).Content.ReadFromJsonAsync<SpreadResponseDto>())!;

        var page1SlotId = spread0.RightPanel.Slots![0].SlotId; // page 1
        var page3SlotId = spread1.RightPanel.Slots![0].SlotId; // page 3

        var assign1 = await _client.PutAsJsonAsync($"/api/binders/{binder.Id}/slots/{page1SlotId}", new { cardVariantId = _variantIds[0] });
        assign1.EnsureSuccessStatusCode();
        var assign2 = await _client.PutAsJsonAsync($"/api/binders/{binder.Id}/slots/{page3SlotId}", new { cardVariantId = _variantIds[1] });
        assign2.EnsureSuccessStatusCode();

        return (binder.Id, page1SlotId, page3SlotId);
    }

    [Fact]
    public async Task BulkSetOwned_MarksSlotsAcrossDifferentPagesInOneCall()
    {
        var (binderId, page1Slot, page3Slot) = await CreateBinderWithTwoAssignedSlotsAcrossSpreadsAsync();

        var response = await _client.PostAsJsonAsync($"/api/binders/{binderId}/slots/bulk-owned", new { slotIds = new[] { page1Slot, page3Slot }, owned = true });
        response.EnsureSuccessStatusCode();
        var result = (await response.Content.ReadFromJsonAsync<BulkUpdateResultDto>())!;
        Assert.Equal(2, result.Updated);

        var spread0 = (await (await _client.GetAsync($"/api/binders/{binderId}/spread/0")).Content.ReadFromJsonAsync<SpreadResponseDto>())!;
        var spread1 = (await (await _client.GetAsync($"/api/binders/{binderId}/spread/1")).Content.ReadFromJsonAsync<SpreadResponseDto>())!;
        Assert.True(spread0.RightPanel.Slots!.Single(s => s.SlotId == page1Slot).Owned);
        Assert.True(spread1.RightPanel.Slots!.Single(s => s.SlotId == page3Slot).Owned);
    }

    [Fact]
    public async Task BulkSetOwned_IgnoresEmptySlotsInTheRequestedList()
    {
        var (binderId, page1Slot, _) = await CreateBinderWithTwoAssignedSlotsAcrossSpreadsAsync();
        var spread2 = (await (await _client.GetAsync($"/api/binders/{binderId}/spread/2")).Content.ReadFromJsonAsync<SpreadResponseDto>())!;
        var emptySlotId = spread2.LeftPanel.Slots![0].SlotId; // page 4, never assigned

        var response = await _client.PostAsJsonAsync($"/api/binders/{binderId}/slots/bulk-owned", new { slotIds = new[] { page1Slot, emptySlotId }, owned = true });
        response.EnsureSuccessStatusCode();
        var result = (await response.Content.ReadFromJsonAsync<BulkUpdateResultDto>())!;

        Assert.Equal(1, result.Updated); // only the filled slot counts
    }

    [Fact]
    public async Task BulkUnassign_ClearsSlotsAcrossDifferentPagesInOneCall()
    {
        var (binderId, page1Slot, page3Slot) = await CreateBinderWithTwoAssignedSlotsAcrossSpreadsAsync();

        var response = await _client.PostAsJsonAsync($"/api/binders/{binderId}/slots/bulk-unassign", new { slotIds = new[] { page1Slot, page3Slot } });
        response.EnsureSuccessStatusCode();
        var result = (await response.Content.ReadFromJsonAsync<BulkUpdateResultDto>())!;
        Assert.Equal(2, result.Updated);

        var spread0 = (await (await _client.GetAsync($"/api/binders/{binderId}/spread/0")).Content.ReadFromJsonAsync<SpreadResponseDto>())!;
        var spread1 = (await (await _client.GetAsync($"/api/binders/{binderId}/spread/1")).Content.ReadFromJsonAsync<SpreadResponseDto>())!;
        Assert.Null(spread0.RightPanel.Slots!.Single(s => s.SlotId == page1Slot).Card);
        Assert.Null(spread1.RightPanel.Slots!.Single(s => s.SlotId == page3Slot).Card);
    }

    [Fact]
    public async Task BulkOperations_OnAnotherUsersBinder_ReturnsNotFound()
    {
        var (binderId, page1Slot, _) = await CreateBinderWithTwoAssignedSlotsAcrossSpreadsAsync();

        var otherClient = _factory.CreateClient();
        var email = $"other-{Guid.NewGuid():N}@pokebinder.test";
        var register = await otherClient.PostAsJsonAsync("/api/auth/register", new { email, password = "OtherUser123!" });
        var body = await register.Content.ReadFromJsonAsync<TokenOnly>();
        otherClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Token);

        var response = await otherClient.PostAsJsonAsync($"/api/binders/{binderId}/slots/bulk-owned", new { slotIds = new[] { page1Slot }, owned = true });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private record TokenOnly(string Token);
}
