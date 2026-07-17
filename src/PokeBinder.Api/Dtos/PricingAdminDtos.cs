namespace PokeBinder.Api.Dtos;

public record QueuedListingDto(
    Guid ClassificationId,
    Guid RawListingId,
    string Title,
    decimal ItemPriceGbp,
    decimal? PostagePriceGbp,
    DateTime SoldDate,
    string ListingFormat,
    string? ThumbnailUrl,
    Guid ResolvedCardVariantId,
    string CardName,
    string SetNumber,
    string VariantTypeName,
    bool IdentityMatchStrong,
    string GradedStatus,
    string? Grader,
    decimal? Grade,
    string RawCondition,
    string VariantMatch,
    string Language,
    bool BestOfferAccepted,
    string? KillReason,
    int ConfidenceScore,
    string Status,
    DateTime ClassifiedAt);

public record ReclassifyRequest(
    string GradedStatus,
    string? Grader,
    decimal? Grade,
    string RawCondition,
    string? Reason);

public record RejectRequest(string? Reason);

public record BulkClassificationActionRequest(Guid[] ClassificationIds, string Action, string? Reason);

public record BulkClassificationActionResultDto(int Succeeded, int Failed);

public record ScrapeRunDto(
    Guid Id,
    DateTime StartedAt,
    DateTime? CompletedAt,
    string Status,
    string TriggeredBy,
    string? TriggeredByUserId,
    int CardsProcessed,
    int ListingsFound,
    int ListingsAccepted,
    int ListingsQuarantined,
    int ListingsRejected,
    string? ErrorMessage);
