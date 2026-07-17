using System.ComponentModel.DataAnnotations;

namespace PokeBinder.Api.Dtos;

public record AssignCardRequest([Required] Guid CardVariantId);

public record MoveSlotRequest([Required] Guid TargetSlotId);

public record UpdateSlotStateRequest(bool? Owned, int? Quantity, string? Condition);

public record SetOverlayTagRequest(Guid? OverlayTagId);

public record BulkAssignRequest(
    [Required, MinLength(1)] IReadOnlyList<Guid> CardVariantIds,
    [Required] Guid StartSlotId,
    [Required, RegularExpression("^(skip|overwrite|fail)$")] string OccupiedStrategy);

public record BulkAssignResultDto(int Placed, int Skipped, int PagesAdded);

public record BulkUpdateOwnedRequest([Required, MinLength(1)] IReadOnlyList<Guid> SlotIds, [Required] bool Owned);

public record BulkUnassignRequest([Required, MinLength(1)] IReadOnlyList<Guid> SlotIds);

public record BulkUpdateResultDto(int Updated);
