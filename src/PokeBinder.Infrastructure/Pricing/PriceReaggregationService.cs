using Microsoft.EntityFrameworkCore;
using PokeBinder.Core.Pricing;

namespace PokeBinder.Infrastructure.Pricing;

/// <summary>
/// Recomputes and replaces one card-variant's PricePoint buckets from its current
/// ListingClassifications. Shared by the orchestrator (after a scrape batch) and the admin review
/// endpoints (after an approve/reclassify/reject, since those flow into the next aggregation
/// immediately rather than waiting for the next scheduled run).
/// </summary>
public class PriceReaggregationService
{
    private readonly PokeBinderDbContext _db;
    private readonly IPriceAggregator _aggregator;

    public PriceReaggregationService(PokeBinderDbContext db, IPriceAggregator aggregator)
    {
        _db = db;
        _aggregator = aggregator;
    }

    public async Task ReaggregateAsync(Guid cardVariantId, CancellationToken ct)
    {
        var listings = await _db.ListingClassifications
            .Where(c => c.ResolvedCardVariantId == cardVariantId)
            .Include(c => c.RawListing)
            .Select(c => new AggregationListing(
                c.GradedStatus, c.Grader, c.Grade, c.RawCondition, c.BestOfferAccepted, c.Language, c.Status,
                c.RawListing!.ItemPriceGbp, c.RawListing.PostagePriceGbp, c.RawListing.SoldDate))
            .ToListAsync(ct);

        var candidates = _aggregator.Aggregate(listings, DateTime.UtcNow);

        var existing = await _db.PricePoints.Where(p => p.CardVariantId == cardVariantId).ToListAsync(ct);
        _db.PricePoints.RemoveRange(existing);

        foreach (var candidate in candidates)
        {
            _db.PricePoints.Add(new PricePoint
            {
                Id = Guid.NewGuid(),
                CardVariantId = cardVariantId,
                GradedStatus = candidate.GradedStatus,
                Grader = candidate.Grader,
                Grade = candidate.Grade,
                Condition = candidate.Condition,
                WindowDays = candidate.WindowDays,
                ItemOnlyMedianGbp = candidate.ItemOnlyMedianGbp,
                DeliveredMedianGbp = candidate.DeliveredMedianGbp,
                SampleCount = candidate.SampleCount,
                MinGbp = candidate.MinGbp,
                MaxGbp = candidate.MaxGbp,
                LastSaleDate = candidate.LastSaleDate,
                QuarantinedReason = candidate.QuarantinedReason,
            });
        }

        await _db.SaveChangesAsync(ct);
    }
}
