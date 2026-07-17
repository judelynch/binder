using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using PokeBinder.Api.Dtos;
using PokeBinder.Core.Cards;
using PokeBinder.Core.Pricing;
using PokeBinder.Infrastructure;
using Xunit;

namespace PokeBinder.Tests;

public class PricingAdminApiTests : IAsyncLifetime
{
    private static readonly string ConnectionString = CustomWebApplicationFactory.ConnectionStringFor("PokeBinderTest_PricingAdmin");

    private CustomWebApplicationFactory _factory = null!;
    private HttpClient _adminClient = null!;
    private HttpClient _userClient = null!;
    private Guid _variantId;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<PokeBinderDbContext>().UseSqlServer(ConnectionString).Options;
        await using (var db = new PokeBinderDbContext(options))
        {
            await db.Database.EnsureDeletedAsync();
            await db.Database.MigrateAsync();

            var set = new Set { Id = "pa-set", Name = "Pricing Admin Set", Series = "Test", PrintedTotal = 1, Total = 1, ReleaseDate = new DateOnly(2022, 1, 1), UpdatedAt = DateTime.UtcNow };
            db.Sets.Add(set);
            var variantType = new VariantType { Id = Guid.NewGuid(), Name = "Normal" };
            db.VariantTypes.Add(variantType);
            var card = new Card { Id = "pa-1", SetId = set.Id, Name = "Eevee", Supertype = "Pokémon", Number = "1" };
            db.Cards.Add(card);
            _variantId = Guid.NewGuid();
            db.CardVariants.Add(new CardVariant { Id = _variantId, CardId = card.Id, VariantTypeId = variantType.Id });

            await db.SaveChangesAsync();
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
        var email = $"pricing-admin-{label}-{Guid.NewGuid():N}@pokebinder.test";
        const string password = "PricingAdminTest123!";

        var register = await client.PostAsJsonAsync("/api/auth/register", new { email, password });
        register.EnsureSuccessStatusCode();

        if (promote)
        {
            await AdminTestHelper.PromoteToAdminAsync(factory, email);
        }

        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        login.EnsureSuccessStatusCode();
        var body = await login.Content.ReadFromJsonAsync<AuthResponseDto>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Token);
        return client;
    }

    [Fact]
    public async Task RunNow_AsAdmin_ReturnsAccepted()
    {
        var response = await _adminClient.PostAsync("/api/admin/pricing/run", null);
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }

    [Fact]
    public async Task RunNow_AsNonAdmin_ReturnsForbidden()
    {
        var response = await _userClient.PostAsync("/api/admin/pricing/run", null);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ScrapeCardNow_WithoutForce_BumpsPriority_DoesNotThrow()
    {
        var response = await _adminClient.PostAsync($"/api/admin/pricing/run/{_variantId}", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var options = new DbContextOptionsBuilder<PokeBinderDbContext>().UseSqlServer(ConnectionString).Options;
        await using var db = new PokeBinderDbContext(options);
        var state = await db.CardVariantScrapeStates.FindAsync(_variantId);
        Assert.NotNull(state);
        Assert.True(state!.ScrapePriority > 0);
        Assert.Null(state.LastScrapedAt); // priority bump alone must not itself perform a scrape
    }

    [Fact]
    public async Task ScrapeCardNow_UnknownVariant_ReturnsNotFound()
    {
        var response = await _adminClient.PostAsync($"/api/admin/pricing/run/{Guid.NewGuid()}", null);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ScrapeCardNow_WithForce_ReturnsAccepted()
    {
        var response = await _adminClient.PostAsync($"/api/admin/pricing/run/{_variantId}?force=true", null);
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }

    // ---- Review queue ----

    private async Task<Guid> SeedQuarantinedListingAsync(DateTime classifiedAt, string title = "Gengar VMAX 66/198 Ultra Rare")
    {
        var options = new DbContextOptionsBuilder<PokeBinderDbContext>().UseSqlServer(ConnectionString).Options;
        await using var db = new PokeBinderDbContext(options);

        var rawListing = new RawListing
        {
            Id = Guid.NewGuid(),
            CardVariantId = _variantId,
            Query = "Gengar 66/198",
            ListingId = $"listing-{Guid.NewGuid():N}",
            SourceProvider = "Mock",
            Title = title,
            ItemPriceGbp = 12.50m,
            PostagePriceGbp = 2.50m,
            SoldDate = DateTime.UtcNow.AddDays(-2),
            ListingFormat = ListingFormat.BuyItNow,
        };
        db.RawListings.Add(rawListing);

        var classification = new ListingClassification
        {
            Id = Guid.NewGuid(),
            RawListingId = rawListing.Id,
            ResolvedCardVariantId = _variantId,
            IdentityMatchStrong = true,
            GradedStatus = GradedStatus.Raw,
            RawCondition = RawConditionClassification.Unspecified,
            VariantMatch = VariantMatch.Ambiguous,
            ConfidenceScore = 50,
            Status = ClassificationStatus.Quarantined,
            ClassifiedAt = classifiedAt,
        };
        db.ListingClassifications.Add(classification);

        await db.SaveChangesAsync();
        return classification.Id;
    }

    [Fact]
    public async Task GetQueue_ReturnsQuarantinedListings_OrderedOldestFirst()
    {
        var older = await SeedQuarantinedListingAsync(DateTime.UtcNow.AddHours(-2));
        var newer = await SeedQuarantinedListingAsync(DateTime.UtcNow.AddHours(-1));

        var response = await _adminClient.GetAsync("/api/admin/pricing/queue?page=1&pageSize=20");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PagedResult<QueuedListingDto>>();

        Assert.NotNull(result);
        Assert.Equal(2, result!.TotalCount);
        Assert.Equal(older, result.Items[0].ClassificationId);
        Assert.Equal(newer, result.Items[1].ClassificationId);
        Assert.Equal("Eevee", result.Items[0].CardName);
    }

    [Fact]
    public async Task ApproveListing_FlipsToAutoAccepted_AndWritesFeedback()
    {
        var classificationId = await SeedQuarantinedListingAsync(DateTime.UtcNow);

        var response = await _adminClient.PostAsJsonAsync($"/api/admin/pricing/queue/{classificationId}/approve", new { reason = (string?)null });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var options = new DbContextOptionsBuilder<PokeBinderDbContext>().UseSqlServer(ConnectionString).Options;
        await using var db = new PokeBinderDbContext(options);
        var classification = await db.ListingClassifications.FindAsync(classificationId);
        Assert.Equal(ClassificationStatus.AutoAccepted, classification!.Status);

        var feedback = await db.ClassificationFeedbacks.SingleAsync(f => f.ListingClassificationId == classificationId);
        Assert.Equal(FeedbackAction.Approved, feedback.Action);
        Assert.Null(feedback.CorrectedValuesJson);
    }

    [Fact]
    public async Task ApproveListing_UnknownId_ReturnsNotFound()
    {
        var response = await _adminClient.PostAsJsonAsync($"/api/admin/pricing/queue/{Guid.NewGuid()}/approve", new { reason = (string?)null });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ReclassifyListing_UpdatesFieldsAndAccepts_AndWritesFeedback()
    {
        var classificationId = await SeedQuarantinedListingAsync(DateTime.UtcNow);

        var response = await _adminClient.PostAsJsonAsync($"/api/admin/pricing/queue/{classificationId}/reclassify", new
        {
            gradedStatus = "Graded",
            grader = "PSA",
            grade = 10m,
            rawCondition = "Unspecified",
            reason = "Title clearly says PSA 10",
        });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var options = new DbContextOptionsBuilder<PokeBinderDbContext>().UseSqlServer(ConnectionString).Options;
        await using var db = new PokeBinderDbContext(options);
        var classification = await db.ListingClassifications.FindAsync(classificationId);
        Assert.Equal(ClassificationStatus.AutoAccepted, classification!.Status);
        Assert.Equal(GradedStatus.Graded, classification.GradedStatus);
        Assert.Equal("PSA", classification.Grader);
        Assert.Equal(10m, classification.Grade);

        var feedback = await db.ClassificationFeedbacks.SingleAsync(f => f.ListingClassificationId == classificationId);
        Assert.Equal(FeedbackAction.Reclassified, feedback.Action);
        Assert.NotNull(feedback.CorrectedValuesJson);
    }

    [Fact]
    public async Task ReclassifyListing_InvalidEnumValue_ReturnsBadRequest()
    {
        var classificationId = await SeedQuarantinedListingAsync(DateTime.UtcNow);

        var response = await _adminClient.PostAsJsonAsync($"/api/admin/pricing/queue/{classificationId}/reclassify", new
        {
            gradedStatus = "NotARealStatus",
            grader = (string?)null,
            grade = (decimal?)null,
            rawCondition = "Unspecified",
            reason = (string?)null,
        });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RejectListing_SetsRejected_AndWritesFeedback()
    {
        var classificationId = await SeedQuarantinedListingAsync(DateTime.UtcNow);

        var response = await _adminClient.PostAsJsonAsync($"/api/admin/pricing/queue/{classificationId}/reject", new { reason = "Bundle/joblot" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var options = new DbContextOptionsBuilder<PokeBinderDbContext>().UseSqlServer(ConnectionString).Options;
        await using var db = new PokeBinderDbContext(options);
        var classification = await db.ListingClassifications.FindAsync(classificationId);
        Assert.Equal(ClassificationStatus.Rejected, classification!.Status);

        var feedback = await db.ClassificationFeedbacks.SingleAsync(f => f.ListingClassificationId == classificationId);
        Assert.Equal(FeedbackAction.Rejected, feedback.Action);
        Assert.Equal("Bundle/joblot", feedback.Reason);
    }

    [Fact]
    public async Task BulkAction_Approve_UpdatesAllAndReportsCounts()
    {
        var first = await SeedQuarantinedListingAsync(DateTime.UtcNow);
        var second = await SeedQuarantinedListingAsync(DateTime.UtcNow);
        var missing = Guid.NewGuid();

        var response = await _adminClient.PostAsJsonAsync("/api/admin/pricing/queue/bulk", new
        {
            classificationIds = new[] { first, second, missing },
            action = "Approve",
            reason = (string?)null,
        });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<BulkClassificationActionResultDto>();
        Assert.Equal(2, result!.Succeeded);
        Assert.Equal(1, result.Failed);

        var options = new DbContextOptionsBuilder<PokeBinderDbContext>().UseSqlServer(ConnectionString).Options;
        await using var db = new PokeBinderDbContext(options);
        Assert.True(await db.ListingClassifications.AllAsync(c => c.Status == ClassificationStatus.AutoAccepted));
    }

    [Fact]
    public async Task BulkAction_InvalidAction_ReturnsBadRequest()
    {
        var response = await _adminClient.PostAsJsonAsync("/api/admin/pricing/queue/bulk", new
        {
            classificationIds = new[] { Guid.NewGuid() },
            action = "Delete",
            reason = (string?)null,
        });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---- Run history ----

    [Fact]
    public async Task GetRunHistory_ReturnsRunsDescendingByStartedAt()
    {
        // Other tests in this class (RunNow/ScrapeCardNow) enqueue real Hangfire jobs against this
        // same shared test database, which can complete and insert their own ScrapeRun row on their
        // own schedule - so this asserts containment/ordering of the two rows it seeded rather than
        // an exact total count, which would be racy against that unrelated background activity.
        var completedId = Guid.NewGuid();
        var runningId = Guid.NewGuid();
        var options = new DbContextOptionsBuilder<PokeBinderDbContext>().UseSqlServer(ConnectionString).Options;
        await using (var db = new PokeBinderDbContext(options))
        {
            db.ScrapeRuns.Add(new ScrapeRun { Id = completedId, StartedAt = DateTime.UtcNow.AddHours(-2), CompletedAt = DateTime.UtcNow.AddHours(-1), Status = ScrapeRunStatus.Completed, TriggeredBy = ScrapeTrigger.Nightly });
            db.ScrapeRuns.Add(new ScrapeRun { Id = runningId, StartedAt = DateTime.UtcNow.AddMinutes(-5), Status = ScrapeRunStatus.Running, TriggeredBy = ScrapeTrigger.Manual });
            await db.SaveChangesAsync();
        }

        var response = await _adminClient.GetAsync("/api/admin/pricing/runs?page=1&pageSize=20");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PagedResult<ScrapeRunDto>>();

        Assert.NotNull(result);
        var completedIndex = result!.Items.ToList().FindIndex(r => r.Id == completedId);
        var runningIndex = result.Items.ToList().FindIndex(r => r.Id == runningId);
        Assert.True(completedIndex >= 0 && runningIndex >= 0, "Both seeded runs should appear in the history.");
        Assert.True(runningIndex < completedIndex, "The more-recently-started run should sort first.");
    }

    private record AuthResponseDto(string Token, string UserId, string Email, IReadOnlyList<string> Roles);
    private record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);
}
