using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using PokeBinder.Api.Dtos;
using PokeBinder.Core.Cards;
using PokeBinder.Infrastructure;
using Xunit;

namespace PokeBinder.Tests;

public class AdminApiTests : IAsyncLifetime
{
    private const string DbName = "PokeBinderTest_Admin";
    private static readonly string ConnectionString = CustomWebApplicationFactory.ConnectionStringFor(DbName);
    private static readonly string UpdatedFixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "CardDataUpdated");

    private CustomWebApplicationFactory _factory = null!;
    private HttpClient _adminClient = null!;
    private HttpClient _userClient = null!;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<PokeBinderDbContext>().UseSqlServer(ConnectionString).Options;
        await using (var db = new PokeBinderDbContext(options))
        {
            await db.Database.EnsureDeletedAsync();
            await db.Database.MigrateAsync();
        }

        _factory = new CustomWebApplicationFactory(ConnectionString);
        _adminClient = await CreateAuthenticatedClientAsync(_factory, "admin", promote: true);
        _userClient = await CreateAuthenticatedClientAsync(_factory, "user", promote: false);
    }

    public async Task DisposeAsync()
    {
        _adminClient.Dispose();
        _userClient.Dispose();
        await _factory.DisposeAsync();
    }

    private static async Task<HttpClient> CreateAuthenticatedClientAsync(CustomWebApplicationFactory factory, string label, bool promote)
    {
        var client = factory.CreateClient();
        var email = $"admin-test-{label}-{Guid.NewGuid():N}@pokebinder.test";
        const string password = "AdminTest123!";

        var register = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, password));
        register.EnsureSuccessStatusCode();

        if (promote)
        {
            await AdminTestHelper.PromoteToAdminAsync(factory, email);
        }

        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        login.EnsureSuccessStatusCode();
        var body = await login.Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Token);
        return client;
    }

    private static async Task<SyncRunDto> ApplySyncAndWaitAsync(HttpClient client, ApplySyncRequest request)
    {
        var start = await client.PostAsJsonAsync("/api/admin/sync/apply", request);
        Assert.Equal(HttpStatusCode.Accepted, start.StatusCode);
        var job = await start.Content.ReadFromJsonAsync<SyncJobStartedDto>();

        var deadline = DateTime.UtcNow.AddSeconds(30);
        while (DateTime.UtcNow < deadline)
        {
            var poll = await client.GetAsync($"/api/admin/sync/jobs/{job!.JobId}");
            poll.EnsureSuccessStatusCode();
            var run = await poll.Content.ReadFromJsonAsync<SyncRunDto>();
            if (run!.Status != "Running")
            {
                return run;
            }
            await Task.Delay(100);
        }

        throw new TimeoutException("Sync job did not complete in time.");
    }

    // ---- Role gating ----

    [Theory]
    [InlineData("POST", "/api/admin/sync/dry-run")]
    [InlineData("POST", "/api/admin/sync/apply")]
    [InlineData("GET", "/api/admin/sync/history?page=1&pageSize=20")]
    [InlineData("GET", "/api/admin/variant-types")]
    [InlineData("POST", "/api/admin/variant-types")]
    [InlineData("POST", "/api/admin/variants/bulk-assign")]
    [InlineData("POST", "/api/admin/sets")]
    public async Task NonAdmin_GetsForbidden(string method, string path)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), path);
        if (method == "POST")
        {
            request.Content = JsonContent.Create(new { });
        }

        var response = await _userClient.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ---- Dry-run vs apply parity ----

    [Fact]
    public async Task DryRun_ReportsExactlyWhatApplyThenPersists()
    {
        var baseline = await ApplySyncAndWaitAsync(_adminClient, new ApplySyncRequest(null, null));
        Assert.Equal("Completed", baseline.Status);
        Assert.Equal(2, baseline.SetsAdded);
        Assert.Equal(9, baseline.CardsAdded);
        Assert.False(string.IsNullOrEmpty(baseline.RunByEmail));

        await using var updatedFactory = new CustomWebApplicationFactory(ConnectionString, UpdatedFixturePath);
        var updatedAdmin = await CreateAuthenticatedClientAsync(updatedFactory, "admin2", promote: true);

        var dryRunResponse = await updatedAdmin.PostAsJsonAsync("/api/admin/sync/dry-run", new { });
        dryRunResponse.EnsureSuccessStatusCode();
        var dryRunSummary = await dryRunResponse.Content.ReadFromJsonAsync<CardImportSummary>();

        Assert.Equal(0, dryRunSummary!.SetsAdded);
        Assert.Equal(0, dryRunSummary.CardsAdded);
        Assert.Equal(1, dryRunSummary.CardsUpdated);
        Assert.Contains(dryRunSummary.ChangedFieldCounts, f => f.Field == "Rarity" && f.Count == 1);

        // Dry run must not have persisted anything.
        await using (var db = new PokeBinderDbContext(new DbContextOptionsBuilder<PokeBinderDbContext>().UseSqlServer(ConnectionString).Options))
        {
            var card = await db.Cards.SingleAsync(c => c.Id == "test1-3");
            Assert.Equal("Rare Holo", card.Rarity);
        }

        var apply = await ApplySyncAndWaitAsync(updatedAdmin, new ApplySyncRequest(null, null));
        Assert.Equal("Completed", apply.Status);
        Assert.Equal(0, apply.SetsAdded);
        Assert.Equal(0, apply.CardsAdded);
        Assert.Equal(1, apply.CardsUpdated);
        Assert.Contains(apply.ChangedFieldCounts, f => f.Field == "Rarity" && f.Count == 1);

        await using (var db = new PokeBinderDbContext(new DbContextOptionsBuilder<PokeBinderDbContext>().UseSqlServer(ConnectionString).Options))
        {
            var card = await db.Cards.SingleAsync(c => c.Id == "test1-3");
            Assert.Equal("Rare Ultra", card.Rarity);
        }
    }

    // ---- Manual-origin conflicts ----

    [Fact]
    public async Task DryRun_SkipsManualOriginConflict_UnlessConfirmed()
    {
        var baseline = await ApplySyncAndWaitAsync(_adminClient, new ApplySyncRequest(null, null));
        Assert.Equal("Completed", baseline.Status);

        await using (var db = new PokeBinderDbContext(new DbContextOptionsBuilder<PokeBinderDbContext>().UseSqlServer(ConnectionString).Options))
        {
            var card = await db.Cards.SingleAsync(c => c.Id == "test1-3");
            card.Origin = DataOrigin.Manual;
            await db.SaveChangesAsync();
        }

        await using var updatedFactory = new CustomWebApplicationFactory(ConnectionString, UpdatedFixturePath);
        var updatedAdmin = await CreateAuthenticatedClientAsync(updatedFactory, "admin3", promote: true);

        var dryRunResponse = await updatedAdmin.PostAsJsonAsync("/api/admin/sync/dry-run", new { });
        var dryRunSummary = await dryRunResponse.Content.ReadFromJsonAsync<CardImportSummary>();

        Assert.Equal(0, dryRunSummary!.CardsUpdated);
        var conflict = Assert.Single(dryRunSummary.ManualConflicts);
        Assert.Equal("test1-3", conflict.EntityId);
        Assert.Contains("Rarity", conflict.ChangedFields);

        // Apply without confirming the override: still skipped.
        var applyUnconfirmed = await ApplySyncAndWaitAsync(updatedAdmin, new ApplySyncRequest(null, null));
        Assert.Equal(0, applyUnconfirmed.CardsUpdated);
        Assert.Single(applyUnconfirmed.RemainingManualConflicts);

        await using (var db = new PokeBinderDbContext(new DbContextOptionsBuilder<PokeBinderDbContext>().UseSqlServer(ConnectionString).Options))
        {
            var card = await db.Cards.SingleAsync(c => c.Id == "test1-3");
            Assert.Equal("Rare Holo", card.Rarity);
        }

        // Apply with the override confirmed: now it goes through.
        var applyConfirmed = await ApplySyncAndWaitAsync(updatedAdmin, new ApplySyncRequest(new[] { "test1-3" }, null));
        Assert.Equal(1, applyConfirmed.CardsUpdated);
        Assert.Empty(applyConfirmed.RemainingManualConflicts);

        await using (var db = new PokeBinderDbContext(new DbContextOptionsBuilder<PokeBinderDbContext>().UseSqlServer(ConnectionString).Options))
        {
            var card = await db.Cards.SingleAsync(c => c.Id == "test1-3");
            Assert.Equal("Rare Ultra", card.Rarity);
        }
    }

    // ---- Bulk variant assignment ----

    [Fact]
    public async Task BulkAssignVariants_RunTwice_SecondRunCreatesNothing()
    {
        await ApplySyncAndWaitAsync(_adminClient, new ApplySyncRequest(null, null));

        var createVariantType = await _adminClient.PostAsJsonAsync("/api/admin/variant-types", new CreateVariantTypeRequest("Reverse Holo Test"));
        createVariantType.EnsureSuccessStatusCode();
        var variantType = await createVariantType.Content.ReadFromJsonAsync<VariantTypeDto>();

        var request = new BulkVariantAssignRequest(
            new CardSearchRequest { SetIds = new[] { "test1" }, Rarities = new[] { "Common" } },
            new[] { variantType!.Id },
            false);

        var first = await _adminClient.PostAsJsonAsync("/api/admin/variants/bulk-assign", request);
        first.EnsureSuccessStatusCode();
        var firstResult = await first.Content.ReadFromJsonAsync<BulkVariantAssignResultDto>();
        Assert.Equal(2, firstResult!.MatchedCards);
        Assert.Equal(2, firstResult.Created);
        Assert.Equal(0, firstResult.Skipped);

        var second = await _adminClient.PostAsJsonAsync("/api/admin/variants/bulk-assign", request);
        second.EnsureSuccessStatusCode();
        var secondResult = await second.Content.ReadFromJsonAsync<BulkVariantAssignResultDto>();
        Assert.Equal(2, secondResult!.MatchedCards);
        Assert.Equal(0, secondResult.Created);
        Assert.Equal(2, secondResult.Skipped);

        await using var db = new PokeBinderDbContext(new DbContextOptionsBuilder<PokeBinderDbContext>().UseSqlServer(ConnectionString).Options);
        var count = await db.CardVariants.CountAsync(v => v.VariantTypeId == variantType.Id);
        Assert.Equal(2, count);
    }

    // ---- Card edit + audit trail ----

    [Fact]
    public async Task UpdateCard_AppliesChanges_AndRecordsAuditEntry()
    {
        await ApplySyncAndWaitAsync(_adminClient, new ApplySyncRequest(null, null));

        var update = await _adminClient.PutAsJsonAsync(
            "/api/admin/cards/test1-3",
            new UpdateCardRequest(null, "Rare Secret", "Corrected Artist", null, null, null, null, "Fixing a known data error"));
        update.EnsureSuccessStatusCode();
        var updated = await update.Content.ReadFromJsonAsync<CardDetailDto>();
        Assert.Equal("Rare Secret", updated!.Rarity);
        Assert.Equal("Corrected Artist", updated.Artist);

        await using (var db = new PokeBinderDbContext(new DbContextOptionsBuilder<PokeBinderDbContext>().UseSqlServer(ConnectionString).Options))
        {
            var card = await db.Cards.SingleAsync(c => c.Id == "test1-3");
            Assert.Equal("Rare Secret", card.Rarity);
            Assert.Equal("Corrected Artist", card.Artist);
        }

        var auditResponse = await _adminClient.GetAsync("/api/admin/cards/test1-3/audit");
        auditResponse.EnsureSuccessStatusCode();
        var audit = await auditResponse.Content.ReadFromJsonAsync<List<CardEditAuditDto>>();
        var entry = Assert.Single(audit!);
        Assert.Equal("Fixing a known data error", entry.Note);
        Assert.Contains("Rarity", entry.ChangedFields);
        Assert.Contains("Artist", entry.ChangedFields);
        Assert.False(string.IsNullOrEmpty(entry.EditedByEmail));

        // A no-op edit (no fields actually changed) is rejected rather than logging an empty audit entry.
        var noOp = await _adminClient.PutAsJsonAsync(
            "/api/admin/cards/test1-3",
            new UpdateCardRequest(null, "Rare Secret", "Corrected Artist", null, null, null, null, "No real change"));
        Assert.Equal(HttpStatusCode.BadRequest, noOp.StatusCode);
    }

    // ---- Delete guards ----

    [Fact]
    public async Task DeletingVariantReferencedByBinderSlot_IsBlockedWithUsageInfo()
    {
        await ApplySyncAndWaitAsync(_adminClient, new ApplySyncRequest(null, null));

        var createVariantType = await _adminClient.PostAsJsonAsync("/api/admin/variant-types", new CreateVariantTypeRequest("Guarded Variant"));
        var variantType = await createVariantType.Content.ReadFromJsonAsync<VariantTypeDto>();

        var addVariant = await _adminClient.PostAsync($"/api/admin/cards/test1-1/variants/{variantType!.Id}", null);
        Assert.Equal(HttpStatusCode.NoContent, addVariant.StatusCode);

        Guid cardVariantId;
        await using (var db = new PokeBinderDbContext(new DbContextOptionsBuilder<PokeBinderDbContext>().UseSqlServer(ConnectionString).Options))
        {
            cardVariantId = await db.CardVariants.Where(v => v.CardId == "test1-1" && v.VariantTypeId == variantType.Id).Select(v => v.Id).SingleAsync();
        }

        var createBinder = await _adminClient.PostAsJsonAsync("/api/binders", new CreateBinderRequest("Guard Test Binder", "#336699", 3, 3, 2));
        createBinder.EnsureSuccessStatusCode();
        var binder = await createBinder.Content.ReadFromJsonAsync<BinderSummaryDto>();

        var spreadResponse = await _adminClient.GetAsync($"/api/binders/{binder!.Id}/spread/0");
        spreadResponse.EnsureSuccessStatusCode();
        var spread = await spreadResponse.Content.ReadFromJsonAsync<SpreadResponseDto>();
        var startSlotId = spread!.RightPanel.Slots![0].SlotId;

        var bulkAssign = await _adminClient.PostAsJsonAsync(
            $"/api/binders/{binder.Id}/slots/bulk-assign",
            new BulkAssignRequest(new[] { cardVariantId }, startSlotId, "skip"));
        bulkAssign.EnsureSuccessStatusCode();

        var deleteVariantType = await _adminClient.DeleteAsync($"/api/admin/variant-types/{variantType.Id}");
        Assert.Equal(HttpStatusCode.Conflict, deleteVariantType.StatusCode);

        var removeCardVariant = await _adminClient.DeleteAsync($"/api/admin/cards/test1-1/variants/{variantType.Id}");
        Assert.Equal(HttpStatusCode.Conflict, removeCardVariant.StatusCode);

        // The variant must still exist untouched after both blocked attempts.
        await using var verifyDb = new PokeBinderDbContext(new DbContextOptionsBuilder<PokeBinderDbContext>().UseSqlServer(ConnectionString).Options);
        Assert.True(await verifyDb.CardVariants.AnyAsync(v => v.Id == cardVariantId));
    }
}
