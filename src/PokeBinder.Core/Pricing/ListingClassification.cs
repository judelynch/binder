namespace PokeBinder.Core.Pricing;

public enum GradedStatus
{
    Raw,
    Graded,
}

/// <summary>
/// A raw listing's stated condition, classified from title text. Deliberately its own enum
/// rather than reusing Binders.CardCondition - that type is nullable-with-null-meaning-unset on
/// BinderSlot, whereas here every classified listing gets a real value, and "the title didn't
/// state a condition" is itself a meaningful classified outcome (Unspecified), not an absence of
/// data. Never assume NM when unstated.
/// </summary>
public enum RawConditionClassification
{
    Unspecified,
    NM,
    LP,
    MP,
    HP,
    DMG,
}

public enum VariantMatch
{
    Confirmed,
    Ambiguous,
    Mismatch,
}

public enum ClassificationStatus
{
    AutoAccepted,
    Quarantined,
    Rejected,
}

/// <summary>
/// Stage 3 output for one RawListing. Re-classifying (e.g. after a scorer change) updates this
/// row in place rather than inserting a new one - there is at most one classification per listing.
/// </summary>
public class ListingClassification
{
    public Guid Id { get; set; }
    public Guid RawListingId { get; set; }
    public RawListing? RawListing { get; set; }

    /// <summary>
    /// The variant this listing's price actually counts toward. Equal to RawListing.CardVariantId
    /// unless the title's variant tokens matched a sibling CardVariant on the same Card instead
    /// (re-attribution) - see IListingClassifier.
    /// </summary>
    public Guid ResolvedCardVariantId { get; set; }

    public bool IdentityMatchStrong { get; set; }
    public GradedStatus GradedStatus { get; set; }
    public string? Grader { get; set; }
    public decimal? Grade { get; set; }
    public RawConditionClassification RawCondition { get; set; } = RawConditionClassification.Unspecified;
    public VariantMatch VariantMatch { get; set; }
    public string Language { get; set; } = "English";
    public bool BestOfferAccepted { get; set; }

    /// <summary>Non-null means this listing was hard-rejected regardless of score - a kill filter matched, the set number was wrong, or the variant mismatched with no in-scope sibling to re-attribute to.</summary>
    public string? KillReason { get; set; }

    public int ConfidenceScore { get; set; }
    public ClassificationStatus Status { get; set; }
    public DateTime ClassifiedAt { get; set; } = DateTime.UtcNow;
}
