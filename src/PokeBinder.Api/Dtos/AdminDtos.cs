using PokeBinder.Core.Cards;

namespace PokeBinder.Api.Dtos;

public record ApplySyncRequest(string[]? ConfirmedOverrideCardIds, string[]? ConfirmedOverrideSetIds);

public record SyncJobStartedDto(Guid JobId);

public record SyncRunDto(
    Guid Id,
    DateTime StartedAt,
    DateTime? CompletedAt,
    string RunByEmail,
    string Status,
    int SetsProcessed,
    int TotalSets,
    int CardsProcessed,
    int SetsAdded,
    int SetsUpdated,
    int CardsAdded,
    int CardsUpdated,
    IReadOnlyList<SyncFieldChange> ChangedFieldCounts,
    IReadOnlyList<SyncManualConflict> RemainingManualConflicts,
    string? ErrorMessage);

public record UpdateCardRequest(
    string? Name,
    string? Rarity,
    string? Artist,
    string? FlavorText,
    string? RegulationMark,
    string? ImageSmallUrl,
    string? ImageLargeUrl,
    string AuditNote);

public record CardEditAuditDto(Guid Id, string EditedByEmail, DateTime EditedAt, string Note, IReadOnlyList<string> ChangedFields);

public record CreateSetRequest(
    string Id,
    string Name,
    string Series,
    int PrintedTotal,
    int Total,
    DateOnly ReleaseDate,
    string? PtcgoCode,
    string? SymbolImageUrl,
    string? LogoImageUrl);

public record CreateCardRequest(
    string Id,
    string Number,
    string Name,
    string Supertype,
    string? Rarity,
    string? Hp,
    IReadOnlyList<string>? Types,
    IReadOnlyList<string>? Subtypes,
    string? Artist,
    string? ImageSmallUrl,
    string? ImageLargeUrl);

public record VariantTypeDto(Guid Id, string Name);

public record CreateVariantTypeRequest(string Name);

public record BulkVariantAssignRequest(CardSearchRequest Filter, Guid[] VariantTypeIds, bool DryRun);

public record BulkVariantAssignResultDto(int MatchedCards, int Created, int Skipped);
