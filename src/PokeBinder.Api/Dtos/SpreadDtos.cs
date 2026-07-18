namespace PokeBinder.Api.Dtos;

public record CardSlotSummaryDto(
    string Id,
    string Name,
    string? ImageSmallUrl,
    string? ImageLargeUrl,
    string SetId,
    string SetName,
    string Number,
    string? Rarity);

public record OverlayTagDto(Guid Id, string Name, string ColourHex);

public record BinderSlotDto(
    Guid SlotId,
    int Position,
    CardSlotSummaryDto? Card,
    Guid? CardVariantId,
    string? VariantTypeName,
    bool Owned,
    int? Quantity,
    string? Condition,
    OverlayTagDto? OverlayTag);

public record SpreadPanelDto(string Type, int? PageNumber, IReadOnlyList<BinderSlotDto>? Slots);

public record SpreadResponseDto(SpreadPanelDto LeftPanel, SpreadPanelDto RightPanel, int TotalSpreads);

public record SuggestedCardDto(
    string CardId,
    string Name,
    string SetId,
    string SetName,
    string Number,
    string? ImageSmallUrl,
    string? Rarity,
    Guid CardVariantId,
    string Reason);

public record SlotSuggestionsDto(Guid SlotId, IReadOnlyList<SuggestedCardDto> Suggestions);
