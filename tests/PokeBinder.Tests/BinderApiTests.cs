using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using PokeBinder.Api.Dtos;
using PokeBinder.Infrastructure;
using Xunit;

namespace PokeBinder.Tests;

public class BinderApiTests : IAsyncLifetime
{
    private static readonly string ConnectionString = CustomWebApplicationFactory.ConnectionStringFor("PokeBinderTest_Binders");

    private CustomWebApplicationFactory _factory = null!;
    private HttpClient _clientA = null!;
    private HttpClient _clientB = null!;
    private List<Guid> _cardVariantIds = null!;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<PokeBinderDbContext>().UseSqlServer(ConnectionString).Options;
        await using (var db = new PokeBinderDbContext(options))
        {
            await db.Database.EnsureDeletedAsync();
            await db.Database.MigrateAsync();
            _cardVariantIds = await CardFixture.SeedCardVariantsAsync(db, 10);
        }

        _factory = new CustomWebApplicationFactory(ConnectionString);
        _clientA = await CreateAuthedClientAsync("owner-a");
        _clientB = await CreateAuthedClientAsync("owner-b");
    }

    public async Task DisposeAsync()
    {
        _clientA.Dispose();
        _clientB.Dispose();
        await _factory.DisposeAsync();
    }

    private async Task<HttpClient> CreateAuthedClientAsync(string label)
    {
        var client = _factory.CreateClient();
        var email = $"{label}-{Guid.NewGuid():N}@pokebinder.test";
        var response = await client.PostAsJsonAsync("/api/auth/register", new { email, password = "TestPass123!" });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<TokenOnly>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Token);
        return client;
    }

    private async Task<BinderSummaryDto> CreateBinderAsync(HttpClient client, int rows, int columns, int initialPageCount)
    {
        var response = await client.PostAsJsonAsync("/api/binders", new
        {
            name = "Test Binder",
            colourHex = "#336699",
            rows,
            columns,
            initialPageCount
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<BinderSummaryDto>())!;
    }

    private async Task<SpreadResponseDto> GetSpreadAsync(HttpClient client, Guid binderId, int spreadIndex)
    {
        var response = await client.GetAsync($"/api/binders/{binderId}/spread/{spreadIndex}");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<SpreadResponseDto>())!;
    }

    [Fact]
    public async Task CreateBinder_FourPages_SpreadMatchesWorkedExample()
    {
        var binder = await CreateBinderAsync(_clientA, rows: 1, columns: 1, initialPageCount: 4);

        var spread0 = await GetSpreadAsync(_clientA, binder.Id, 0);
        Assert.Equal("cover", spread0.LeftPanel.Type);
        Assert.Equal("page", spread0.RightPanel.Type);
        Assert.Equal(1, spread0.RightPanel.PageNumber);
        Assert.Equal(3, spread0.TotalSpreads);

        var spread1 = await GetSpreadAsync(_clientA, binder.Id, 1);
        Assert.Equal(2, spread1.LeftPanel.PageNumber);
        Assert.Equal(3, spread1.RightPanel.PageNumber);

        var spread2 = await GetSpreadAsync(_clientA, binder.Id, 2);
        Assert.Equal(4, spread2.LeftPanel.PageNumber);
        Assert.Equal("cover", spread2.RightPanel.Type);
    }

    [Fact]
    public async Task CreateBinder_OddInitialPageCount_ReturnsBadRequest()
    {
        var response = await _clientA.PostAsJsonAsync("/api/binders", new
        {
            name = "Odd Binder",
            colourHex = "#336699",
            rows = 1,
            columns = 1,
            initialPageCount = 3
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetSpread_UpdatesLastAccessedAt()
    {
        var binder = await CreateBinderAsync(_clientA, 1, 1, 2);
        await GetSpreadAsync(_clientA, binder.Id, 0);

        var list = await (await _clientA.GetAsync("/api/binders")).Content.ReadFromJsonAsync<List<BinderSummaryDto>>();
        var reloaded = list!.Single(b => b.Id == binder.Id);
        Assert.NotNull(reloaded.LastAccessedAt);
    }

    [Fact]
    public async Task DeletePage_RemovesWholeSheetAndRenumbersSubsequentPages()
    {
        // 1x1 binder, 8 pages. Tag old page 5 slot 0 with a distinctive card so we
        // can prove it's still findable at its new page number after renumbering.
        var binder = await CreateBinderAsync(_clientA, rows: 1, columns: 1, initialPageCount: 8);
        var spreadBefore = await GetSpreadAsync(_clientA, binder.Id, 2); // spread2 = page4+page5
        var page5SlotId = spreadBefore.RightPanel.Slots!.Single().SlotId;

        var assignResponse = await _clientA.PutAsJsonAsync(
            $"/api/binders/{binder.Id}/slots/{page5SlotId}", new { cardVariantId = _cardVariantIds[0] });
        assignResponse.EnsureSuccessStatusCode();

        // Delete the sheet containing page 3 (pages 3+4, both empty) -> pages 5-8 renumber down to 3-6.
        var deleteResponse = await _clientA.DeleteAsync($"/api/binders/{binder.Id}/pages/3");
        deleteResponse.EnsureSuccessStatusCode();
        var afterDelete = await deleteResponse.Content.ReadFromJsonAsync<BinderSummaryDto>();
        Assert.Equal(6, afterDelete!.PageCount);

        // Old page 5 is now page 3 -> spread1 = page2+page3.
        var spreadAfter = await GetSpreadAsync(_clientA, binder.Id, 1);
        Assert.Equal(3, spreadAfter.RightPanel.PageNumber);
        var movedSlot = spreadAfter.RightPanel.Slots!.Single();
        Assert.NotNull(movedSlot.Card);
        Assert.Equal("fixture-set-1", movedSlot.Card!.Id); // _cardVariantIds[0] is the Normal variant of fixture-set-1
    }

    [Fact]
    public async Task DeletePage_WithAssignedSlots_RequiresForce()
    {
        var binder = await CreateBinderAsync(_clientA, rows: 1, columns: 1, initialPageCount: 4);
        var spread0 = await GetSpreadAsync(_clientA, binder.Id, 0);
        var page1SlotId = spread0.RightPanel.Slots!.Single().SlotId;

        await _clientA.PutAsJsonAsync($"/api/binders/{binder.Id}/slots/{page1SlotId}", new { cardVariantId = _cardVariantIds[0] });

        var withoutForce = await _clientA.DeleteAsync($"/api/binders/{binder.Id}/pages/1");
        Assert.Equal(HttpStatusCode.Conflict, withoutForce.StatusCode);

        var withForce = await _clientA.DeleteAsync($"/api/binders/{binder.Id}/pages/1?force=true");
        Assert.Equal(HttpStatusCode.OK, withForce.StatusCode);
        var summary = await withForce.Content.ReadFromJsonAsync<BinderSummaryDto>();
        Assert.Equal(2, summary!.PageCount);
    }

    [Fact]
    public async Task MoveSlot_SwapsBothSlotsInOneOperation()
    {
        var binder = await CreateBinderAsync(_clientA, rows: 2, columns: 2, initialPageCount: 2);
        var spread = await GetSpreadAsync(_clientA, binder.Id, 0);
        var slots = spread.RightPanel.Slots!;
        var slotA = slots[0];
        var slotB = slots[1];

        await _clientA.PutAsJsonAsync($"/api/binders/{binder.Id}/slots/{slotA.SlotId}", new { cardVariantId = _cardVariantIds[0] });
        await _clientA.PutAsJsonAsync($"/api/binders/{binder.Id}/slots/{slotB.SlotId}", new { cardVariantId = _cardVariantIds[1] });

        var moveResponse = await _clientA.PostAsJsonAsync(
            $"/api/binders/{binder.Id}/slots/{slotA.SlotId}/move", new { targetSlotId = slotB.SlotId });
        moveResponse.EnsureSuccessStatusCode();

        var after = await GetSpreadAsync(_clientA, binder.Id, 0);
        var afterA = after.RightPanel.Slots!.Single(s => s.SlotId == slotA.SlotId);
        var afterB = after.RightPanel.Slots!.Single(s => s.SlotId == slotB.SlotId);

        Assert.Equal("fixture-set-2", afterA.Card!.Id);
        Assert.Equal("fixture-set-1", afterB.Card!.Id);
    }

    [Fact]
    public async Task BulkMove_MovesWholeSelectionToConsecutiveSlotsAcrossNonAdjacentPages()
    {
        var binder = await CreateBinderAsync(_clientA, rows: 2, columns: 2, initialPageCount: 6); // pages 1-6
        var page1 = await GetSpreadAsync(_clientA, binder.Id, 0);
        var slots = page1.RightPanel.Slots!;

        await _clientA.PutAsJsonAsync($"/api/binders/{binder.Id}/slots/{slots[0].SlotId}", new { cardVariantId = _cardVariantIds[0] });
        await _clientA.PutAsJsonAsync($"/api/binders/{binder.Id}/slots/{slots[1].SlotId}", new { cardVariantId = _cardVariantIds[1] });

        var page3 = await GetSpreadAsync(_clientA, binder.Id, 1); // spread 1 = pages 2-3
        var dropSlotId = page3.RightPanel.Slots![2].SlotId; // page 3, position 2

        var response = await _clientA.PostAsJsonAsync($"/api/binders/{binder.Id}/slots/bulk-move", new
        {
            sourceSlotIds = new[] { slots[1].SlotId, slots[0].SlotId }, // deliberately reversed - server re-sorts to binder order
            startSlotId = dropSlotId,
        });
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<BulkMoveResultDto>();
        Assert.Equal(2, result!.Moved);

        var options = new DbContextOptionsBuilder<PokeBinderDbContext>().UseSqlServer(ConnectionString).Options;
        await using var db = new PokeBinderDbContext(options);

        var sourceA = await db.BinderSlots.FindAsync(slots[0].SlotId);
        var sourceB = await db.BinderSlots.FindAsync(slots[1].SlotId);
        Assert.Null(sourceA!.CardVariantId);
        Assert.Null(sourceB!.CardVariantId);

        var landedAtDrop = await db.BinderSlots.Include(s => s.Page)
            .FirstAsync(s => s.Page!.BinderId == binder.Id && s.Page.PageNumber == 3 && s.Position == 2);
        var landedNext = await db.BinderSlots.Include(s => s.Page)
            .FirstAsync(s => s.Page!.BinderId == binder.Id && s.Page.PageNumber == 3 && s.Position == 3);
        // Earliest-positioned selected card (slots[0], variant 0) lands exactly on the drop point.
        Assert.Equal(_cardVariantIds[0], landedAtDrop.CardVariantId);
        Assert.Equal(_cardVariantIds[1], landedNext.CardVariantId);
    }

    [Fact]
    public async Task BulkMove_SkipsSlotsThatAreThemselvesInTheSelection()
    {
        var binder = await CreateBinderAsync(_clientA, rows: 1, columns: 4, initialPageCount: 2);
        var spread = await GetSpreadAsync(_clientA, binder.Id, 0);
        var slots = spread.RightPanel.Slots!; // page 1, positions 0-3

        await _clientA.PutAsJsonAsync($"/api/binders/{binder.Id}/slots/{slots[0].SlotId}", new { cardVariantId = _cardVariantIds[0] });
        await _clientA.PutAsJsonAsync($"/api/binders/{binder.Id}/slots/{slots[1].SlotId}", new { cardVariantId = _cardVariantIds[1] });
        await _clientA.PutAsJsonAsync($"/api/binders/{binder.Id}/slots/{slots[2].SlotId}", new { cardVariantId = _cardVariantIds[2] });

        // Drop point is slots[0] itself - one of the selected sources. Walking forward should skip
        // past every other selected slot too (1 and 2), landing the block starting at slot 3.
        var response = await _clientA.PostAsJsonAsync($"/api/binders/{binder.Id}/slots/bulk-move", new
        {
            sourceSlotIds = new[] { slots[0].SlotId, slots[1].SlotId, slots[2].SlotId },
            startSlotId = slots[0].SlotId,
        });
        response.EnsureSuccessStatusCode();

        var options = new DbContextOptionsBuilder<PokeBinderDbContext>().UseSqlServer(ConnectionString).Options;
        await using var db = new PokeBinderDbContext(options);

        var landedAt3 = await db.BinderSlots.Include(s => s.Page)
            .FirstAsync(s => s.Page!.BinderId == binder.Id && s.Page.PageNumber == 1 && s.Position == 3);
        var landedAtPage2Pos0 = await db.BinderSlots.Include(s => s.Page)
            .FirstAsync(s => s.Page!.BinderId == binder.Id && s.Page.PageNumber == 2 && s.Position == 0);
        var landedAtPage2Pos1 = await db.BinderSlots.Include(s => s.Page)
            .FirstAsync(s => s.Page!.BinderId == binder.Id && s.Page.PageNumber == 2 && s.Position == 1);

        Assert.Equal(_cardVariantIds[0], landedAt3.CardVariantId);
        Assert.Equal(_cardVariantIds[1], landedAtPage2Pos0.CardVariantId);
        Assert.Equal(_cardVariantIds[2], landedAtPage2Pos1.CardVariantId);
    }

    [Fact]
    public async Task BulkMove_RunningPastTheLastPage_AutoAddsPages()
    {
        var binder = await CreateBinderAsync(_clientA, rows: 1, columns: 2, initialPageCount: 2); // 4 slots total
        var page1 = await GetSpreadAsync(_clientA, binder.Id, 0);
        var slots = page1.RightPanel.Slots!;
        var page2 = await GetSpreadAsync(_clientA, binder.Id, 1);
        // Page count is even (2), so the last spread is page 2 LEFT + back cover RIGHT (see
        // CLAUDE.md's binder rendering rules) - page 2's slots are on the left, not the right.
        var page2Slots = page2.LeftPanel.Slots!;

        await _clientA.PutAsJsonAsync($"/api/binders/{binder.Id}/slots/{slots[0].SlotId}", new { cardVariantId = _cardVariantIds[0] });
        await _clientA.PutAsJsonAsync($"/api/binders/{binder.Id}/slots/{slots[1].SlotId}", new { cardVariantId = _cardVariantIds[1] });
        await _clientA.PutAsJsonAsync($"/api/binders/{binder.Id}/slots/{page2Slots[0].SlotId}", new { cardVariantId = _cardVariantIds[2] });

        // Drop point is the last slot in the binder (page2Slots[1]) - moving all 3 selected cards
        // there needs 3 target slots, but only 1 remains, so the binder must grow.
        var response = await _clientA.PostAsJsonAsync($"/api/binders/{binder.Id}/slots/bulk-move", new
        {
            sourceSlotIds = new[] { slots[0].SlotId, slots[1].SlotId, page2Slots[0].SlotId },
            startSlotId = page2Slots[1].SlotId,
        });
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<BulkMoveResultDto>();

        Assert.Equal(3, result!.Moved);
        Assert.True(result.PagesAdded > 0);

        var binderAfter = await _clientA.GetAsync($"/api/binders/{binder.Id}");
        binderAfter.EnsureSuccessStatusCode();
        var summary = await binderAfter.Content.ReadFromJsonAsync<BinderSummaryDto>();
        Assert.True(summary!.PageCount > 2);
    }

    [Fact]
    public async Task BulkMove_UnknownSourceSlot_ReturnsNotFound()
    {
        var binder = await CreateBinderAsync(_clientA, rows: 2, columns: 2, initialPageCount: 2);
        var spread = await GetSpreadAsync(_clientA, binder.Id, 0);
        var slots = spread.RightPanel.Slots!;

        var response = await _clientA.PostAsJsonAsync($"/api/binders/{binder.Id}/slots/bulk-move", new
        {
            sourceSlotIds = new[] { Guid.NewGuid() },
            startSlotId = slots[0].SlotId,
        });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task BulkAssign_Skip_PlacesAroundOccupiedSlots()
    {
        var binder = await CreateBinderAsync(_clientA, rows: 1, columns: 4, initialPageCount: 2);
        var spread = await GetSpreadAsync(_clientA, binder.Id, 0);
        var slots = spread.RightPanel.Slots!;

        // Occupy position 1 up front.
        await _clientA.PutAsJsonAsync($"/api/binders/{binder.Id}/slots/{slots[1].SlotId}", new { cardVariantId = _cardVariantIds[9] });

        var bulkResponse = await _clientA.PostAsJsonAsync($"/api/binders/{binder.Id}/slots/bulk-assign", new
        {
            cardVariantIds = new[] { _cardVariantIds[0], _cardVariantIds[1] },
            startSlotId = slots[0].SlotId,
            occupiedStrategy = "skip"
        });
        bulkResponse.EnsureSuccessStatusCode();
        var result = await bulkResponse.Content.ReadFromJsonAsync<BulkAssignResultDto>();
        Assert.Equal(2, result!.Placed);
        Assert.Equal(1, result.Skipped);

        var after = await GetSpreadAsync(_clientA, binder.Id, 0);
        var afterSlots = after.RightPanel.Slots!;
        Assert.Equal("fixture-set-1", afterSlots[0].Card!.Id);
        Assert.Equal("fixture-set-10", afterSlots[1].Card!.Id); // untouched, still the pre-occupied card
        Assert.Equal("fixture-set-2", afterSlots[2].Card!.Id);
    }

    [Fact]
    public async Task BulkAssign_Fail_AbortsEntirelyAndWritesNothing()
    {
        var binder = await CreateBinderAsync(_clientA, rows: 1, columns: 3, initialPageCount: 2);
        var spread = await GetSpreadAsync(_clientA, binder.Id, 0);
        var slots = spread.RightPanel.Slots!;

        await _clientA.PutAsJsonAsync($"/api/binders/{binder.Id}/slots/{slots[1].SlotId}", new { cardVariantId = _cardVariantIds[9] });

        var bulkResponse = await _clientA.PostAsJsonAsync($"/api/binders/{binder.Id}/slots/bulk-assign", new
        {
            cardVariantIds = new[] { _cardVariantIds[0], _cardVariantIds[1], _cardVariantIds[2] },
            startSlotId = slots[0].SlotId,
            occupiedStrategy = "fail"
        });
        Assert.Equal(HttpStatusCode.Conflict, bulkResponse.StatusCode);

        var after = await GetSpreadAsync(_clientA, binder.Id, 0);
        var afterSlots = after.RightPanel.Slots!;
        Assert.Null(afterSlots[0].Card); // would have been written first in the walk, but nothing should persist
        Assert.Equal("fixture-set-10", afterSlots[1].Card!.Id); // original assignment untouched
    }

    [Fact]
    public async Task BulkAssign_RunsOutOfSlots_AutoAppendsPages()
    {
        var binder = await CreateBinderAsync(_clientA, rows: 1, columns: 1, initialPageCount: 2); // 2 slots total
        var spread = await GetSpreadAsync(_clientA, binder.Id, 0);
        var startSlotId = spread.RightPanel.Slots!.Single().SlotId;

        var bulkResponse = await _clientA.PostAsJsonAsync($"/api/binders/{binder.Id}/slots/bulk-assign", new
        {
            cardVariantIds = _cardVariantIds.Take(5).ToArray(), // needs 5 slots, only 2 exist
            startSlotId,
            occupiedStrategy = "overwrite"
        });
        bulkResponse.EnsureSuccessStatusCode();
        var result = await bulkResponse.Content.ReadFromJsonAsync<BulkAssignResultDto>();
        Assert.Equal(5, result!.Placed);
        Assert.True(result.PagesAdded > 0);

        var list = await (await _clientA.GetAsync("/api/binders")).Content.ReadFromJsonAsync<List<BinderSummaryDto>>();
        var reloaded = list!.Single(b => b.Id == binder.Id);
        Assert.True(reloaded.PageCount >= 5);
        Assert.Equal(0, reloaded.PageCount % 2); // still even
        Assert.Equal(5, reloaded.FilledSlots);
    }

    [Fact]
    public async Task OwnershipIsolation_OtherUserCannotAccessBinder()
    {
        var binder = await CreateBinderAsync(_clientA, rows: 1, columns: 1, initialPageCount: 2);

        Assert.Equal(HttpStatusCode.NotFound, (await _clientB.GetAsync($"/api/binders/{binder.Id}/spread/0")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _clientB.PatchAsync($"/api/binders/{binder.Id}",
            JsonContent.Create(new { name = "Hijacked" }))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _clientB.DeleteAsync($"/api/binders/{binder.Id}")).StatusCode);

        var listB = await (await _clientB.GetAsync("/api/binders")).Content.ReadFromJsonAsync<List<BinderSummaryDto>>();
        Assert.DoesNotContain(listB!, b => b.Id == binder.Id);

        // Binder A's data must be untouched.
        var listA = await (await _clientA.GetAsync("/api/binders")).Content.ReadFromJsonAsync<List<BinderSummaryDto>>();
        Assert.Contains(listA!, b => b.Id == binder.Id && b.Name == "Test Binder");
    }

    [Fact]
    public async Task Dashboard_ReconcilesWithSlotData()
    {
        var binder = await CreateBinderAsync(_clientA, rows: 1, columns: 4, initialPageCount: 2);
        var spread = await GetSpreadAsync(_clientA, binder.Id, 0);
        var slots = spread.RightPanel.Slots!;

        await _clientA.PutAsJsonAsync($"/api/binders/{binder.Id}/slots/{slots[0].SlotId}", new { cardVariantId = _cardVariantIds[0] });
        await _clientA.PatchAsJsonAsync($"/api/binders/{binder.Id}/slots/{slots[0].SlotId}", new { owned = true, quantity = 3 });

        await _clientA.PutAsJsonAsync($"/api/binders/{binder.Id}/slots/{slots[1].SlotId}", new { cardVariantId = _cardVariantIds[1] });
        // assigned but not owned -> counts as missing

        await _clientA.PutAsJsonAsync($"/api/binders/{binder.Id}/slots/{slots[2].SlotId}", new { cardVariantId = _cardVariantIds[2] });
        await _clientA.PatchAsJsonAsync($"/api/binders/{binder.Id}/slots/{slots[2].SlotId}", new { owned = true });

        var dashboardResponse = await _clientA.GetAsync("/api/dashboard");
        dashboardResponse.EnsureSuccessStatusCode();
        var dashboard = await dashboardResponse.Content.ReadFromJsonAsync<DashboardResponseDto>();

        Assert.Equal(4, dashboard!.CardsOwned); // quantity 3 + default 1
        Assert.Equal(1, dashboard.CardsMissing);
        Assert.Equal(1, dashboard.BinderCount);

        var recent = dashboard.RecentBinders.Single(b => b.Id == binder.Id);
        Assert.Equal(66.7, recent.CompletenessPercent, 1); // 2 owned / 3 assigned
    }

    [Fact]
    public async Task UnassignSlot_ClearsCardOwnedQuantityConditionButKeepsOverlayTag()
    {
        var binder = await CreateBinderAsync(_clientA, rows: 1, columns: 1, initialPageCount: 2);
        var spread = await GetSpreadAsync(_clientA, binder.Id, 0);
        var slotId = spread.RightPanel.Slots!.Single().SlotId;

        var tagResponse = await _clientA.PostAsJsonAsync("/api/overlay-tags", new { name = "Ordered", colourHex = "#ff0000" });
        tagResponse.EnsureSuccessStatusCode();
        var tag = await tagResponse.Content.ReadFromJsonAsync<OverlayTagDto>();

        await _clientA.PutAsJsonAsync($"/api/binders/{binder.Id}/slots/{slotId}", new { cardVariantId = _cardVariantIds[0] });
        await _clientA.PatchAsJsonAsync($"/api/binders/{binder.Id}/slots/{slotId}", new { owned = true, quantity = 2, condition = "NM" });
        await _clientA.PatchAsJsonAsync($"/api/binders/{binder.Id}/slots/{slotId}/overlay-tag", new { overlayTagId = tag!.Id });

        var deleteResponse = await _clientA.DeleteAsync($"/api/binders/{binder.Id}/slots/{slotId}");
        deleteResponse.EnsureSuccessStatusCode();
        var afterDelete = await deleteResponse.Content.ReadFromJsonAsync<BinderSlotDto>();

        Assert.Null(afterDelete!.Card);
        Assert.False(afterDelete.Owned);
        Assert.Null(afterDelete.Quantity);
        Assert.Null(afterDelete.Condition);
        Assert.NotNull(afterDelete.OverlayTag); // tag survives unassignment
        Assert.Equal(tag.Id, afterDelete.OverlayTag!.Id);

        // And clearing the tag explicitly does remove it.
        var clearResponse = await _clientA.PatchAsJsonAsync($"/api/binders/{binder.Id}/slots/{slotId}/overlay-tag", new { overlayTagId = (Guid?)null });
        clearResponse.EnsureSuccessStatusCode();
        var afterClear = await clearResponse.Content.ReadFromJsonAsync<BinderSlotDto>();
        Assert.Null(afterClear!.OverlayTag);
    }

    [Fact]
    public async Task OverlayTags_CrudRoundTrip()
    {
        var createResponse = await _clientA.PostAsJsonAsync("/api/overlay-tags", new { name = "Wishlist", colourHex = "#00ff00" });
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<OverlayTagDto>();

        var listResponse = await _clientA.GetAsync("/api/overlay-tags");
        var list = await listResponse.Content.ReadFromJsonAsync<List<OverlayTagDto>>();
        Assert.Contains(list!, t => t.Id == created!.Id && t.Name == "Wishlist");

        var updateResponse = await _clientA.PatchAsJsonAsync($"/api/overlay-tags/{created!.Id}", new { colourHex = "#0000ff" });
        updateResponse.EnsureSuccessStatusCode();
        var updated = await updateResponse.Content.ReadFromJsonAsync<OverlayTagDto>();
        Assert.Equal("#0000ff", updated!.ColourHex);

        var deleteResponse = await _clientA.DeleteAsync($"/api/overlay-tags/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var listAfter = await (await _clientA.GetAsync("/api/overlay-tags")).Content.ReadFromJsonAsync<List<OverlayTagDto>>();
        Assert.DoesNotContain(listAfter!, t => t.Id == created.Id);
    }

    [Fact]
    public async Task DeleteBinder_RemovesItAndItsPagesAndSlots()
    {
        var binder = await CreateBinderAsync(_clientA, rows: 1, columns: 1, initialPageCount: 2);

        var deleteResponse = await _clientA.DeleteAsync($"/api/binders/{binder.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        Assert.Equal(HttpStatusCode.NotFound, (await _clientA.GetAsync($"/api/binders/{binder.Id}/spread/0")).StatusCode);

        var list = await (await _clientA.GetAsync("/api/binders")).Content.ReadFromJsonAsync<List<BinderSummaryDto>>();
        Assert.DoesNotContain(list!, b => b.Id == binder.Id);
    }

    private record TokenOnly(string Token);
}
