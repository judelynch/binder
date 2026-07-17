namespace PokeBinder.Core.Pricing;

/// <summary>A single already-classified, already-resolved-to-this-variant listing, flattened for aggregation math - no EF entities, so this is directly unit-testable.</summary>
public record AggregationListing(
    GradedStatus GradedStatus,
    string? Grader,
    decimal? Grade,
    RawConditionClassification RawCondition,
    bool BestOfferAccepted,
    string Language,
    ClassificationStatus Status,
    decimal ItemPriceGbp,
    decimal? PostagePriceGbp,
    DateTime SoldDate);

public record PricePointCandidate(
    GradedStatus GradedStatus,
    string? Grader,
    decimal? Grade,
    RawConditionClassification? Condition,
    int WindowDays,
    decimal ItemOnlyMedianGbp,
    decimal DeliveredMedianGbp,
    int SampleCount,
    decimal MinGbp,
    decimal MaxGbp,
    DateTime LastSaleDate,
    string? QuarantinedReason);

/// <summary>
/// Stage 4: buckets one card-variant's accepted listings by (GradedStatus[/Grader/Grade or
/// Condition]) x rolling window, and computes a robust median price per bucket. Pure function of
/// its inputs - no DB - the orchestrator is responsible for gathering "everything ever accepted
/// for this variant", not just what one scrape batch touched, since a listing from three weeks
/// ago is still inside a 30-day window today.
/// </summary>
public interface IPriceAggregator
{
    IReadOnlyList<PricePointCandidate> Aggregate(IReadOnlyList<AggregationListing> listings, DateTime now);
}
