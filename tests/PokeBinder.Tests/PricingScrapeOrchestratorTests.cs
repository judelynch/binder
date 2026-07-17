using Microsoft.EntityFrameworkCore;
using PokeBinder.Core.Binders;
using PokeBinder.Core.Cards;
using PokeBinder.Core.Pricing;
using PokeBinder.Infrastructure;
using PokeBinder.Infrastructure.Pricing;
using Xunit;

namespace PokeBinder.Tests;

public class PricingScrapeOrchestratorTests : IAsyncLifetime
{
    private static readonly string ConnectionString = CustomWebApplicationFactory.ConnectionStringFor("PokeBinderTest_PricingScrape");
    private Guid _binderId;
    private int _pageCounter;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<PokeBinderDbContext>().UseSqlServer(ConnectionString).Options;
        await using var db = new PokeBinderDbContext(options);
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();

        var binder = new Binder { Id = Guid.NewGuid(), OwnerId = "scrape-owner", Name = "Scrape Binder", ColourHex = "#000000", Rows = 3, Columns = 3, CreatedAt = DateTime.UtcNow };
        db.Binders.Add(binder);
        _binderId = binder.Id;
        await db.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private PokeBinderDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<PokeBinderDbContext>().UseSqlServer(ConnectionString).Options);

    /// <summary>Seeds a Card + Normal CardVariant and puts it in scope via a binder slot. Each call uses a fresh page so slots never collide.</summary>
    private async Task<Guid> SeedInScopeVariantAsync(PokeBinderDbContext db, string cardId, string name = "Gengar")
    {
        var setId = $"set-{cardId}";
        if (!await db.Sets.AnyAsync(s => s.Id == setId))
        {
            db.Sets.Add(new Set { Id = setId, Name = "Fusion Strike", Series = "Sword & Shield", PrintedTotal = 107, Total = 107, ReleaseDate = new DateOnly(2021, 1, 1), UpdatedAt = DateTime.UtcNow });
        }

        var variantTypeId = await db.VariantTypes.Where(v => v.Name == "Normal").Select(v => v.Id).FirstOrDefaultAsync();
        if (variantTypeId == Guid.Empty)
        {
            variantTypeId = Guid.NewGuid();
            db.VariantTypes.Add(new VariantType { Id = variantTypeId, Name = "Normal" });
        }

        db.Cards.Add(new Card { Id = cardId, SetId = setId, Name = name, Supertype = "Pokémon", Number = "66" });
        var variantId = Guid.NewGuid();
        db.CardVariants.Add(new CardVariant { Id = variantId, CardId = cardId, VariantTypeId = variantTypeId });

        var page = new BinderPage { Id = Guid.NewGuid(), BinderId = _binderId, PageNumber = ++_pageCounter };
        db.BinderPages.Add(page);
        db.BinderSlots.Add(new BinderSlot { Id = Guid.NewGuid(), PageId = page.Id, Position = 0, CardVariantId = variantId, Owned = false });

        await db.SaveChangesAsync();
        return variantId;
    }

    private static PricingScrapeOrchestrator CreateOrchestrator(PokeBinderDbContext db, PricingScrapeOptions? options = null) =>
        new(db, new BinderScrapeScopeProvider(db), new MockPriceSourceProvider(), new ListingClassifier(new ClassifierOptions()),
            new PriceReaggregationService(db, new PriceAggregator()),
            options ?? new PricingScrapeOptions { RequestDelaySeconds = 0, RequestJitterMaxSeconds = 0, BatchPauseSeconds = 0 });

    [Fact]
    public async Task FreshnessSkip_DoesNotRescrapeWithin24h()
    {
        await using var db = CreateContext();
        var variantId = await SeedInScopeVariantAsync(db, "fresh-1");
        db.CardVariantScrapeStates.Add(new CardVariantScrapeState { CardVariantId = variantId, LastScrapedAt = DateTime.UtcNow.AddHours(-1) });
        await db.SaveChangesAsync();

        var orchestrator = CreateOrchestrator(db);
        await orchestrator.RunAsync(ScrapeTrigger.Manual, null);

        var run = await db.ScrapeRuns.SingleAsync();
        Assert.Equal(0, run.CardsProcessed);
    }

    [Fact]
    public async Task StalestFirst_NullLastScrapedBeatsOlderTimestamp()
    {
        await using var db = CreateContext();
        var neverScraped = await SeedInScopeVariantAsync(db, "stale-null");
        var scraped48hAgo = await SeedInScopeVariantAsync(db, "stale-48h");
        var scraped30hAgo = await SeedInScopeVariantAsync(db, "stale-30h");
        db.CardVariantScrapeStates.AddRange(
            new CardVariantScrapeState { CardVariantId = scraped48hAgo, LastScrapedAt = DateTime.UtcNow.AddHours(-48) },
            new CardVariantScrapeState { CardVariantId = scraped30hAgo, LastScrapedAt = DateTime.UtcNow.AddHours(-30) });
        await db.SaveChangesAsync();

        var orchestrator = CreateOrchestrator(db, new PricingScrapeOptions { RequestDelaySeconds = 0, RequestJitterMaxSeconds = 0, BatchPauseSeconds = 0, MaxCardsPerRun = 2 });
        await orchestrator.RunAsync(ScrapeTrigger.Manual, null);

        var processed = await db.CardVariantScrapeStates.Where(s => s.LastScrapedAt >= DateTime.UtcNow.AddMinutes(-1)).Select(s => s.CardVariantId).ToListAsync();
        Assert.Contains(neverScraped, processed);
        Assert.Contains(scraped48hAgo, processed);
        Assert.DoesNotContain(scraped30hAgo, processed);
    }

    [Fact]
    public async Task ScrapePriority_BumpedVariant_ProcessedFirst()
    {
        await using var db = CreateContext();
        var bumped = await SeedInScopeVariantAsync(db, "priority-bumped");
        var normal = await SeedInScopeVariantAsync(db, "priority-normal");
        db.CardVariantScrapeStates.AddRange(
            new CardVariantScrapeState { CardVariantId = bumped, ScrapePriority = 10 },
            new CardVariantScrapeState { CardVariantId = normal, ScrapePriority = 0 });
        await db.SaveChangesAsync();

        var orchestrator = CreateOrchestrator(db, new PricingScrapeOptions { RequestDelaySeconds = 0, RequestJitterMaxSeconds = 0, BatchPauseSeconds = 0, MaxCardsPerRun = 1 });
        await orchestrator.RunAsync(ScrapeTrigger.Manual, null);

        var bumpedState = await db.CardVariantScrapeStates.FindAsync(bumped);
        var normalState = await db.CardVariantScrapeStates.FindAsync(normal);
        Assert.NotNull(bumpedState!.LastScrapedAt);
        Assert.Null(normalState!.LastScrapedAt);
    }

    [Fact]
    public async Task ScrapePriority_ResetToZeroAfterProcessing()
    {
        await using var db = CreateContext();
        var variantId = await SeedInScopeVariantAsync(db, "priority-reset");
        db.CardVariantScrapeStates.Add(new CardVariantScrapeState { CardVariantId = variantId, ScrapePriority = 5 });
        await db.SaveChangesAsync();

        var orchestrator = CreateOrchestrator(db);
        await orchestrator.RunAsync(ScrapeTrigger.Manual, null);

        var state = await db.CardVariantScrapeStates.FindAsync(variantId);
        Assert.Equal(0, state!.ScrapePriority);
    }

    [Fact]
    public async Task AllTargets_ProcessedRegardlessOfBatchSize()
    {
        await using var db = CreateContext();
        for (var i = 0; i < 5; i++)
        {
            await SeedInScopeVariantAsync(db, $"batch-{i}");
        }

        var orchestrator = CreateOrchestrator(db, new PricingScrapeOptions { RequestDelaySeconds = 0, RequestJitterMaxSeconds = 0, BatchPauseSeconds = 0, BatchSize = 2 });
        await orchestrator.RunAsync(ScrapeTrigger.Manual, null);

        var run = await db.ScrapeRuns.SingleAsync();
        Assert.Equal(5, run.CardsProcessed);
    }

    [Fact]
    public async Task BatchPause_ElapsesBetweenBatches()
    {
        await using var db = CreateContext();
        for (var i = 0; i < 3; i++)
        {
            await SeedInScopeVariantAsync(db, $"pause-{i}");
        }

        // 3 items, batch size 2 -> 2 batches -> exactly one pause gap of ~1s.
        var orchestrator = CreateOrchestrator(db, new PricingScrapeOptions { RequestDelaySeconds = 0, RequestJitterMaxSeconds = 0, BatchPauseSeconds = 1, BatchSize = 2 });
        var started = DateTime.UtcNow;
        await orchestrator.RunAsync(ScrapeTrigger.Manual, null);
        var elapsed = DateTime.UtcNow - started;

        Assert.True(elapsed.TotalSeconds >= 1, $"expected at least a 1s batch pause, elapsed {elapsed.TotalSeconds}s");
    }

    [Fact]
    public async Task RunLock_SecondCallNoOpsWhileFirstRunIsActive()
    {
        await using var db = CreateContext();
        db.ScrapeRuns.Add(new ScrapeRun { Id = Guid.NewGuid(), StartedAt = DateTime.UtcNow, Status = ScrapeRunStatus.Running, TriggeredBy = ScrapeTrigger.Manual });
        await db.SaveChangesAsync();

        var orchestrator = CreateOrchestrator(db);
        await orchestrator.RunAsync(ScrapeTrigger.Manual, null);

        var runCount = await db.ScrapeRuns.CountAsync();
        Assert.Equal(1, runCount); // no second run was created
        var stillRunning = await db.ScrapeRuns.SingleAsync();
        Assert.Equal(ScrapeRunStatus.Running, stillRunning.Status);
    }

    [Fact]
    public async Task StaleLockRecovery_OldRunningRunMarkedFailed_NewRunProceeds()
    {
        await using var db = CreateContext();
        db.ScrapeRuns.Add(new ScrapeRun { Id = Guid.NewGuid(), StartedAt = DateTime.UtcNow.AddHours(-3), Status = ScrapeRunStatus.Running, TriggeredBy = ScrapeTrigger.Nightly });
        await db.SaveChangesAsync();

        var orchestrator = CreateOrchestrator(db, new PricingScrapeOptions { RequestDelaySeconds = 0, RequestJitterMaxSeconds = 0, BatchPauseSeconds = 0, StaleLockTimeoutMinutes = 120 });
        await orchestrator.RunAsync(ScrapeTrigger.Manual, null);

        var runs = await db.ScrapeRuns.OrderBy(r => r.StartedAt).ToListAsync();
        Assert.Equal(2, runs.Count);
        Assert.Equal(ScrapeRunStatus.Failed, runs[0].Status);
        Assert.Equal(ScrapeRunStatus.Completed, runs[1].Status);
    }

    [Fact]
    public async Task ShouldRunCatchUp_NoRunsAtAll_ReturnsTrue()
    {
        await using var db = CreateContext();
        var orchestrator = CreateOrchestrator(db);
        Assert.True(await orchestrator.ShouldRunCatchUpAsync(CancellationToken.None));
    }

    [Fact]
    public async Task ShouldRunCatchUp_RecentCompletion_ReturnsFalse()
    {
        await using var db = CreateContext();
        db.ScrapeRuns.Add(new ScrapeRun { Id = Guid.NewGuid(), StartedAt = DateTime.UtcNow.AddHours(-2), CompletedAt = DateTime.UtcNow.AddHours(-1), Status = ScrapeRunStatus.Completed, TriggeredBy = ScrapeTrigger.Nightly });
        await db.SaveChangesAsync();

        var orchestrator = CreateOrchestrator(db);
        Assert.False(await orchestrator.ShouldRunCatchUpAsync(CancellationToken.None));
    }

    [Fact]
    public async Task ShouldRunCatchUp_ActiveRunInProgress_ReturnsFalse_NeverDoubleEnqueues()
    {
        await using var db = CreateContext();
        db.ScrapeRuns.Add(new ScrapeRun { Id = Guid.NewGuid(), StartedAt = DateTime.UtcNow, Status = ScrapeRunStatus.Running, TriggeredBy = ScrapeTrigger.Manual });
        await db.SaveChangesAsync();

        var orchestrator = CreateOrchestrator(db);
        Assert.False(await orchestrator.ShouldRunCatchUpAsync(CancellationToken.None));
    }

    [Fact]
    public async Task MaxCardsPerRun_LimitsTargetCount()
    {
        await using var db = CreateContext();
        for (var i = 0; i < 5; i++)
        {
            await SeedInScopeVariantAsync(db, $"max-{i}");
        }

        var orchestrator = CreateOrchestrator(db, new PricingScrapeOptions { RequestDelaySeconds = 0, RequestJitterMaxSeconds = 0, BatchPauseSeconds = 0, MaxCardsPerRun = 3 });
        await orchestrator.RunAsync(ScrapeTrigger.Manual, null);

        var run = await db.ScrapeRuns.SingleAsync();
        Assert.Equal(3, run.CardsProcessed);
    }

    [Fact]
    public async Task ForceScrape_BypassesFreshnessCheck()
    {
        await using var db = CreateContext();
        var variantId = await SeedInScopeVariantAsync(db, "force-fresh");
        db.CardVariantScrapeStates.Add(new CardVariantScrapeState { CardVariantId = variantId, LastScrapedAt = DateTime.UtcNow.AddMinutes(-1) });
        await db.SaveChangesAsync();

        var orchestrator = CreateOrchestrator(db);
        await orchestrator.RunAsync(ScrapeTrigger.Manual, null, forceCardVariantId: variantId);

        var run = await db.ScrapeRuns.SingleAsync();
        Assert.Equal(1, run.CardsProcessed);
    }

    [Fact]
    public async Task EndToEnd_ProducesPricePointsAndConsistentRunCounts()
    {
        await using var db = CreateContext();
        var variantId = await SeedInScopeVariantAsync(db, "e2e-1");

        var orchestrator = CreateOrchestrator(db);
        await orchestrator.RunAsync(ScrapeTrigger.Manual, null);

        var run = await db.ScrapeRuns.SingleAsync();
        Assert.Equal(ScrapeRunStatus.Completed, run.Status);
        Assert.Equal(run.ListingsFound, run.ListingsAccepted + run.ListingsQuarantined + run.ListingsRejected);
        Assert.True(run.ListingsFound > 0);

        var pricePoints = await db.PricePoints.Where(p => p.CardVariantId == variantId).ToListAsync();
        // MockPriceSourceProvider always produces clean, well-formed listings, so with >=3 in any
        // window they should auto-accept and aggregate into at least one bucket.
        Assert.NotEmpty(pricePoints);
    }
}
