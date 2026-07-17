using Microsoft.EntityFrameworkCore;
using PokeBinder.Core.Cards;
using PokeBinder.Core.Pricing;

namespace PokeBinder.Infrastructure.Pricing;

/// <summary>
/// The Hangfire job body. One run = pick the stalest in-scope card-variants, fetch+classify each,
/// then re-aggregate every variant touched (including any re-attribution targets). Batch pacing
/// (pause between batches, delay+jitter between individual requests) is enforced here regardless
/// of provider - kept even against the mock so this timing behavior is actually exercised and
/// doesn't need to change the day a real provider replaces the mock.
/// </summary>
public class PricingScrapeOrchestrator
{
    private readonly PokeBinderDbContext _db;
    private readonly IScrapeScopeProvider _scope;
    private readonly IPriceSourceProvider _priceSource;
    private readonly IListingClassifier _classifier;
    private readonly PriceReaggregationService _reaggregation;
    private readonly PricingScrapeOptions _options;
    private static readonly Random Jitter = new();

    public PricingScrapeOrchestrator(
        PokeBinderDbContext db,
        IScrapeScopeProvider scope,
        IPriceSourceProvider priceSource,
        IListingClassifier classifier,
        PriceReaggregationService reaggregation,
        PricingScrapeOptions options)
    {
        _db = db;
        _scope = scope;
        _priceSource = priceSource;
        _classifier = classifier;
        _reaggregation = reaggregation;
        _options = options;
    }

    /// <summary>True if a catch-up run should be enqueued: no run has COMPLETED in the freshness window and none is currently in flight.</summary>
    public async Task<bool> ShouldRunCatchUpAsync(CancellationToken ct)
    {
        var cutoff = DateTime.UtcNow.AddHours(-_options.FreshnessHours);
        var hasRecentCompletion = await _db.ScrapeRuns.AnyAsync(r => r.Status == ScrapeRunStatus.Completed && r.CompletedAt >= cutoff, ct);
        if (hasRecentCompletion)
        {
            return false;
        }

        var hasActiveRun = await GetActiveRunAsync(ct) is not null;
        return !hasActiveRun;
    }

    public async Task RunAsync(ScrapeTrigger trigger, string? triggeredByUserId, Guid? forceCardVariantId = null, CancellationToken ct = default)
    {
        if (await GetActiveRunAsync(ct) is not null)
        {
            return; // another run is genuinely in flight - never run two at once
        }

        var run = new ScrapeRun
        {
            Id = Guid.NewGuid(),
            StartedAt = DateTime.UtcNow,
            Status = ScrapeRunStatus.Running,
            TriggeredBy = trigger,
            TriggeredByUserId = triggeredByUserId,
        };
        _db.ScrapeRuns.Add(run);
        await _db.SaveChangesAsync(ct);

        try
        {
            var targets = await SelectTargetsAsync(forceCardVariantId, ct);
            var touchedVariantIds = new HashSet<Guid>();

            for (var i = 0; i < targets.Count; i += _options.BatchSize)
            {
                var batch = targets.Skip(i).Take(_options.BatchSize).ToList();
                foreach (var variantId in batch)
                {
                    await ProcessVariantAsync(variantId, run, touchedVariantIds, ct);
                    run.CardsProcessed++;
                    await _db.SaveChangesAsync(ct);

                    if (_options.RequestDelaySeconds > 0 || _options.RequestJitterMaxSeconds > 0)
                    {
                        var jitter = _options.RequestJitterMaxSeconds > 0 ? Jitter.Next(0, _options.RequestJitterMaxSeconds + 1) : 0;
                        await Task.Delay(TimeSpan.FromSeconds(_options.RequestDelaySeconds + jitter), ct);
                    }
                }

                if (i + _options.BatchSize < targets.Count && _options.BatchPauseSeconds > 0)
                {
                    await Task.Delay(TimeSpan.FromSeconds(_options.BatchPauseSeconds), ct);
                }
            }

            foreach (var variantId in touchedVariantIds)
            {
                await _reaggregation.ReaggregateAsync(variantId, ct);
            }

            run.Status = ScrapeRunStatus.Completed;
            run.CompletedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            run.Status = ScrapeRunStatus.Failed;
            run.CompletedAt = DateTime.UtcNow;
            run.ErrorMessage = ex.Message;
            await _db.SaveChangesAsync(ct);
            throw;
        }
    }

    private async Task<ScrapeRun?> GetActiveRunAsync(CancellationToken ct)
    {
        var running = await _db.ScrapeRuns.Where(r => r.Status == ScrapeRunStatus.Running).ToListAsync(ct);
        if (running.Count == 0)
        {
            return null;
        }

        var staleCutoff = DateTime.UtcNow.AddMinutes(-_options.StaleLockTimeoutMinutes);
        var active = running.Where(r => r.StartedAt >= staleCutoff).ToList();
        var stale = running.Where(r => r.StartedAt < staleCutoff).ToList();

        foreach (var staleRun in stale)
        {
            staleRun.Status = ScrapeRunStatus.Failed;
            staleRun.CompletedAt = DateTime.UtcNow;
            staleRun.ErrorMessage = "Run exceeded the stale-lock timeout and was assumed crashed.";
        }
        if (stale.Count > 0)
        {
            await _db.SaveChangesAsync(ct);
        }

        return active.FirstOrDefault();
    }

    private async Task<List<Guid>> SelectTargetsAsync(Guid? forceCardVariantId, CancellationToken ct)
    {
        if (forceCardVariantId is { } forced)
        {
            await EnsureScrapeStateExistsAsync(forced, ct);
            return new List<Guid> { forced };
        }

        var inScope = await _scope.GetInScopeCardVariantIdsAsync(ct);
        foreach (var variantId in inScope)
        {
            await EnsureScrapeStateExistsAsync(variantId, ct);
        }

        var freshnessCutoff = DateTime.UtcNow.AddHours(-_options.FreshnessHours);
        var states = await _db.CardVariantScrapeStates
            .Where(s => inScope.Contains(s.CardVariantId))
            .Where(s => s.LastScrapedAt == null || s.LastScrapedAt < freshnessCutoff)
            .OrderByDescending(s => s.ScrapePriority)
            .ThenBy(s => s.LastScrapedAt) // nulls sort first in SQL Server ascending order
            .Take(_options.MaxCardsPerRun)
            .Select(s => s.CardVariantId)
            .ToListAsync(ct);

        return states;
    }

    private async Task EnsureScrapeStateExistsAsync(Guid cardVariantId, CancellationToken ct)
    {
        var exists = await _db.CardVariantScrapeStates.AnyAsync(s => s.CardVariantId == cardVariantId, ct);
        if (!exists)
        {
            _db.CardVariantScrapeStates.Add(new CardVariantScrapeState { CardVariantId = cardVariantId });
            await _db.SaveChangesAsync(ct);
        }
    }

    private async Task ProcessVariantAsync(Guid cardVariantId, ScrapeRun run, HashSet<Guid> touchedVariantIds, CancellationToken ct)
    {
        var variant = await _db.CardVariants
            .Include(v => v.VariantType)
            .Include(v => v.Card).ThenInclude(c => c!.Set)
            .Include(v => v.Card).ThenInclude(c => c!.Variants).ThenInclude(v => v.VariantType)
            .FirstOrDefaultAsync(v => v.Id == cardVariantId, ct);

        if (variant?.Card is null || variant.VariantType is null)
        {
            return;
        }

        var card = variant.Card;
        var setNumber = $"{card.Number}/{card.Set?.PrintedTotal.ToString() ?? "?"}";
        var query = new PriceQuery(cardVariantId, card.Name, setNumber, variant.VariantType.Name);

        var listings = await _priceSource.GetListingsAsync(query, ct);
        run.ListingsFound += listings.Count;

        var inScope = new HashSet<Guid>(await _scope.GetInScopeCardVariantIdsAsync(ct));
        var siblings = card.Variants
            .Where(v => v.Id != cardVariantId && v.VariantType is not null)
            .Select(v => new SiblingVariant(v.Id, v.VariantType!.Name))
            .ToList();

        foreach (var listing in listings)
        {
            var rawListing = await _db.RawListings.FirstOrDefaultAsync(
                l => l.ListingId == listing.ListingId && l.SourceProvider == _priceSource.ProviderName, ct);

            if (rawListing is null)
            {
                rawListing = new RawListing
                {
                    Id = Guid.NewGuid(),
                    CardVariantId = cardVariantId,
                    Query = $"{query.CardName} {query.SetNumber}",
                    ListingId = listing.ListingId,
                    SourceProvider = _priceSource.ProviderName,
                    Title = listing.Title,
                    ItemPriceGbp = listing.ItemPriceGbp,
                    PostagePriceGbp = listing.PostagePriceGbp,
                    SoldDate = listing.SoldDate,
                    ListingFormat = listing.ListingFormat,
                    ThumbnailUrl = listing.ThumbnailUrl,
                };
                _db.RawListings.Add(rawListing);
                await _db.SaveChangesAsync(ct);
            }

            var input = new ListingClassificationInput(
                Title: listing.Title,
                ListingFormat: listing.ListingFormat,
                CardName: card.Name,
                SetName: card.Set?.Name ?? string.Empty,
                SetNumber: setNumber,
                TargetVariantTypeName: variant.VariantType.Name,
                SiblingVariants: siblings,
                InScopeCardVariantIds: inScope);

            var result = _classifier.Classify(cardVariantId, input);

            var classification = await _db.ListingClassifications.FirstOrDefaultAsync(c => c.RawListingId == rawListing.Id, ct);
            if (classification is null)
            {
                classification = new ListingClassification { Id = Guid.NewGuid(), RawListingId = rawListing.Id };
                _db.ListingClassifications.Add(classification);
            }

            classification.ResolvedCardVariantId = result.ResolvedCardVariantId;
            classification.IdentityMatchStrong = result.IdentityMatchStrong;
            classification.GradedStatus = result.GradedStatus;
            classification.Grader = result.Grader;
            classification.Grade = result.Grade;
            classification.RawCondition = result.RawCondition;
            classification.VariantMatch = result.VariantMatch;
            classification.Language = result.Language;
            classification.BestOfferAccepted = result.BestOfferAccepted;
            classification.KillReason = result.KillReason;
            classification.ConfidenceScore = result.ConfidenceScore;
            classification.Status = result.Status;
            classification.ClassifiedAt = DateTime.UtcNow;

            touchedVariantIds.Add(result.ResolvedCardVariantId);

            switch (result.Status)
            {
                case ClassificationStatus.AutoAccepted:
                    run.ListingsAccepted++;
                    break;
                case ClassificationStatus.Quarantined:
                    run.ListingsQuarantined++;
                    break;
                case ClassificationStatus.Rejected:
                    run.ListingsRejected++;
                    break;
            }
        }

        await _db.SaveChangesAsync(ct);

        var state = await _db.CardVariantScrapeStates.FirstAsync(s => s.CardVariantId == cardVariantId, ct);
        state.LastScrapedAt = DateTime.UtcNow;
        state.ScrapePriority = 0; // a manual "scrape now" bump is spent once serviced, win or lose
        await _db.SaveChangesAsync(ct);
    }
}
