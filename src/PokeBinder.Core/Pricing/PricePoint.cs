namespace PokeBinder.Core.Pricing;

/// <summary>
/// Stage 4 aggregated output: one row per (CardVariant x GradedStatus[/Grader/Grade or
/// Condition] x rolling window). Each aggregation run replaces its own buckets wholesale rather
/// than incrementally updating them, since the underlying listing set for a window shifts every
/// day regardless of whether a new scrape ran.
/// </summary>
public class PricePoint
{
    public Guid Id { get; set; }
    public Guid CardVariantId { get; set; }

    public GradedStatus GradedStatus { get; set; }
    public string? Grader { get; set; }
    public decimal? Grade { get; set; }
    public RawConditionClassification? Condition { get; set; }

    public int WindowDays { get; set; }

    public decimal ItemOnlyMedianGbp { get; set; }
    public decimal DeliveredMedianGbp { get; set; }
    public int SampleCount { get; set; }
    public decimal MinGbp { get; set; }
    public decimal MaxGbp { get; set; }
    public DateTime LastSaleDate { get; set; }

    /// <summary>Non-null means this bucket failed a sanity check and is held back from display - never published to the UI while set.</summary>
    public string? QuarantinedReason { get; set; }

    public DateTime AggregatedAt { get; set; } = DateTime.UtcNow;
}
