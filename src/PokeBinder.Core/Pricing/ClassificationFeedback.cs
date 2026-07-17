namespace PokeBinder.Core.Pricing;

public enum FeedbackAction
{
    Approved,
    Reclassified,
    Rejected,
}

/// <summary>
/// Audit trail for every manual review decision on a ListingClassification - future training
/// data, per Phase 8. OriginalGuess/CorrectedValues are plain JSON snapshots of the
/// classification dimensions (not a strongly-typed column) since this table is written-to and
/// read-back-for-display only, never filtered/joined on its snapshot contents.
/// </summary>
public class ClassificationFeedback
{
    public Guid Id { get; set; }
    public Guid ListingClassificationId { get; set; }
    public ListingClassification? ListingClassification { get; set; }

    public string OriginalGuessJson { get; set; } = string.Empty;

    /// <summary>Null when the action was a plain Approve (nothing changed).</summary>
    public string? CorrectedValuesJson { get; set; }

    public FeedbackAction Action { get; set; }
    public string? Reason { get; set; }

    public string ReviewedByUserId { get; set; } = string.Empty;
    public DateTime ReviewedAt { get; set; } = DateTime.UtcNow;
}
