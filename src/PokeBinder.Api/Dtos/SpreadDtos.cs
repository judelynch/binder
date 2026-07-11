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
    string? VariantTypeName,
    bool Owned,
    int? Quantity,
    string? Condition,
    OverlayTagDto? OverlayTag);

public record SpreadPanelDto(string Type, int? PageNumber, IReadOnlyList<BinderSlotDto>? Slots);

public record SpreadResponseDto(SpreadPanelDto LeftPanel, SpreadPanelDto RightPanel, int TotalSpreads);
