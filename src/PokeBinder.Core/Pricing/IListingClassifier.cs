namespace PokeBinder.Core.Pricing;

public record SiblingVariant(Guid CardVariantId, string VariantTypeName);

public record ListingClassificationInput(
    string Title,
    ListingFormat ListingFormat,
    string CardName,
    string SetName,
    string SetNumber,
    string TargetVariantTypeName,
    IReadOnlyList<SiblingVariant> SiblingVariants,
    IReadOnlySet<Guid> InScopeCardVariantIds);

public record ClassificationResult(
    Guid ResolvedCardVariantId,
    bool IdentityMatchStrong,
    GradedStatus GradedStatus,
    string? Grader,
    decimal? Grade,
    RawConditionClassification RawCondition,
    VariantMatch VariantMatch,
    string Language,
    bool BestOfferAccepted,
    string? KillReason,
    int ConfidenceScore,
    ClassificationStatus Status);

/// <summary>
/// Pure rule-based scorer - no DB, no I/O, entirely a function of its inputs, so it's directly
/// unit-testable against real-shaped titles without any fixture/database setup.
/// </summary>
public interface IListingClassifier
{
    ClassificationResult Classify(Guid queriedCardVariantId, ListingClassificationInput input);
}
