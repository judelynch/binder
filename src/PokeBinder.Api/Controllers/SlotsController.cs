using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PokeBinder.Api.Dtos;
using PokeBinder.Core.Binders;
using PokeBinder.Infrastructure;

namespace PokeBinder.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/binders/{binderId:guid}/slots")]
public class SlotsController : ControllerBase
{
    private readonly PokeBinderDbContext _db;

    public SlotsController(PokeBinderDbContext db)
    {
        _db = db;
    }

    [HttpPut("{slotId:guid}")]
    public async Task<ActionResult<BinderSlotDto>> Assign(Guid binderId, Guid slotId, AssignCardRequest request, CancellationToken ct)
    {
        if (!await OwnsBinderAsync(binderId, ct))
        {
            return NotFound();
        }

        var slot = await LoadSlotAsync(binderId, slotId, ct);
        if (slot is null)
        {
            return NotFound();
        }

        var variantExists = await _db.CardVariants.AnyAsync(v => v.Id == request.CardVariantId, ct);
        if (!variantExists)
        {
            return ValidationProblem(BuildModelState(nameof(request.CardVariantId), "Card variant does not exist."));
        }

        slot.CardVariantId = request.CardVariantId;
        slot.Owned = false;
        slot.Quantity = null;
        slot.Condition = null;

        await _db.SaveChangesAsync(ct);
        await ReloadSlotDetailsAsync(slot, ct);
        return Ok(SlotMapping.ToDto(slot));
    }

    [HttpDelete("{slotId:guid}")]
    public async Task<ActionResult<BinderSlotDto>> Unassign(Guid binderId, Guid slotId, CancellationToken ct)
    {
        if (!await OwnsBinderAsync(binderId, ct))
        {
            return NotFound();
        }

        var slot = await LoadSlotAsync(binderId, slotId, ct);
        if (slot is null)
        {
            return NotFound();
        }

        slot.CardVariantId = null;
        slot.Owned = false;
        slot.Quantity = null;
        slot.Condition = null;

        await _db.SaveChangesAsync(ct);
        await ReloadSlotDetailsAsync(slot, ct);
        return Ok(SlotMapping.ToDto(slot));
    }

    [HttpPatch("{slotId:guid}")]
    public async Task<ActionResult<BinderSlotDto>> UpdateState(Guid binderId, Guid slotId, UpdateSlotStateRequest request, CancellationToken ct)
    {
        if (!await OwnsBinderAsync(binderId, ct))
        {
            return NotFound();
        }

        var slot = await LoadSlotAsync(binderId, slotId, ct);
        if (slot is null)
        {
            return NotFound();
        }

        if (request.Owned.HasValue)
        {
            slot.Owned = request.Owned.Value;
        }

        if (request.Quantity.HasValue)
        {
            slot.Quantity = request.Quantity.Value;
        }

        if (request.Condition is not null)
        {
            if (!Enum.TryParse<CardCondition>(request.Condition, out var condition))
            {
                return ValidationProblem(BuildModelState(nameof(request.Condition), "Condition must be one of NM, LP, MP, HP, DMG."));
            }

            slot.Condition = condition;
        }

        await _db.SaveChangesAsync(ct);
        await ReloadSlotDetailsAsync(slot, ct);
        return Ok(SlotMapping.ToDto(slot));
    }

    [HttpPatch("{slotId:guid}/overlay-tag")]
    public async Task<ActionResult<BinderSlotDto>> SetOverlayTag(Guid binderId, Guid slotId, SetOverlayTagRequest request, CancellationToken ct)
    {
        if (!await OwnsBinderAsync(binderId, ct))
        {
            return NotFound();
        }

        var slot = await LoadSlotAsync(binderId, slotId, ct);
        if (slot is null)
        {
            return NotFound();
        }

        if (request.OverlayTagId is not null)
        {
            var userId = this.GetUserId();
            var tagExists = await _db.OverlayTags.AnyAsync(t => t.Id == request.OverlayTagId && t.OwnerId == userId, ct);
            if (!tagExists)
            {
                return ValidationProblem(BuildModelState(nameof(request.OverlayTagId), "Overlay tag does not exist."));
            }
        }

        slot.OverlayTagId = request.OverlayTagId;

        await _db.SaveChangesAsync(ct);
        await ReloadSlotDetailsAsync(slot, ct);
        return Ok(SlotMapping.ToDto(slot));
    }

    [HttpPost("{slotId:guid}/move")]
    public async Task<IActionResult> Move(Guid binderId, Guid slotId, MoveSlotRequest request, CancellationToken ct)
    {
        if (!await OwnsBinderAsync(binderId, ct))
        {
            return NotFound();
        }

        if (slotId == request.TargetSlotId)
        {
            return ValidationProblem(BuildModelState(nameof(request.TargetSlotId), "Target slot must be different from the source slot."));
        }

        var source = await LoadSlotAsync(binderId, slotId, ct);
        var target = await LoadSlotAsync(binderId, request.TargetSlotId, ct);
        if (source is null || target is null)
        {
            return NotFound();
        }

        await using var transaction = await _db.Database.BeginTransactionAsync(ct);

        (source.CardVariantId, target.CardVariantId) = (target.CardVariantId, source.CardVariantId);
        (source.Owned, target.Owned) = (target.Owned, source.Owned);
        (source.Quantity, target.Quantity) = (target.Quantity, source.Quantity);
        (source.Condition, target.Condition) = (target.Condition, source.Condition);
        (source.OverlayTagId, target.OverlayTagId) = (target.OverlayTagId, source.OverlayTagId);

        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        await ReloadSlotDetailsAsync(source, ct);
        await ReloadSlotDetailsAsync(target, ct);

        return Ok(new { source = SlotMapping.ToDto(source), target = SlotMapping.ToDto(target) });
    }

    [HttpPost("bulk-assign")]
    public async Task<ActionResult<BulkAssignResultDto>> BulkAssign(Guid binderId, BulkAssignRequest request, CancellationToken ct)
    {
        if (!Enum.TryParse<OccupiedStrategy>(request.OccupiedStrategy, ignoreCase: true, out var strategy))
        {
            return ValidationProblem(BuildModelState(nameof(request.OccupiedStrategy), "Must be one of skip, overwrite, fail."));
        }

        var userId = this.GetUserId();
        var binder = await _db.Binders
            .Include(b => b.Pages).ThenInclude(p => p.Slots)
            .FirstOrDefaultAsync(b => b.Id == binderId && b.OwnerId == userId, ct);

        if (binder is null)
        {
            return NotFound();
        }

        var requestedIds = request.CardVariantIds.Distinct().ToList();
        var validIdCount = await _db.CardVariants.CountAsync(v => requestedIds.Contains(v.Id), ct);
        if (validIdCount != requestedIds.Count)
        {
            return ValidationProblem(BuildModelState(nameof(request.CardVariantIds), "One or more card variants do not exist."));
        }

        var orderedSlots = binder.Pages.OrderBy(p => p.PageNumber).SelectMany(p => p.Slots.OrderBy(s => s.Position)).ToList();
        var startIndex = orderedSlots.FindIndex(s => s.Id == request.StartSlotId);
        if (startIndex < 0)
        {
            return NotFound();
        }

        var existingOccupied = orderedSlots.Select(s => s.CardVariantId != null).ToList();

        BulkAssignPlan plan;
        try
        {
            plan = BulkAssignPlanner.Plan(existingOccupied, startIndex, request.CardVariantIds, strategy);
        }
        catch (BulkAssignConflictException)
        {
            return Conflict(new { message = "One or more target slots are already occupied." });
        }

        var pagesAdded = 0;
        var maxIndexUsed = plan.Placements.Count == 0 ? -1 : plan.Placements.Max(p => p.SlotIndex);
        if (maxIndexUsed >= orderedSlots.Count)
        {
            var slotsPerPage = binder.Rows * binder.Columns;
            var virtualSlotsNeeded = maxIndexUsed - orderedSlots.Count + 1;
            var rawPagesNeeded = (int)Math.Ceiling(virtualSlotsNeeded / (double)slotsPerPage);
            pagesAdded = BinderPageFactory.RoundUpToEven(rawPagesNeeded);

            var nextPageNumber = binder.Pages.Count == 0 ? 1 : binder.Pages.Max(p => p.PageNumber) + 1;
            BinderPageFactory.AppendPages(_db, binder, nextPageNumber, pagesAdded);

            orderedSlots = binder.Pages.OrderBy(p => p.PageNumber).SelectMany(p => p.Slots.OrderBy(s => s.Position)).ToList();
        }

        foreach (var placement in plan.Placements)
        {
            var slot = orderedSlots[placement.SlotIndex];
            slot.CardVariantId = placement.CardVariantId;
            slot.Owned = false;
            slot.Quantity = null;
            slot.Condition = null;
        }

        await _db.SaveChangesAsync(ct);

        return Ok(new BulkAssignResultDto(plan.Placements.Count, plan.SkippedOccupiedSlots, pagesAdded));
    }

    private async Task<bool> OwnsBinderAsync(Guid binderId, CancellationToken ct)
    {
        var userId = this.GetUserId();
        return await _db.Binders.AnyAsync(b => b.Id == binderId && b.OwnerId == userId, ct);
    }

    private Task<BinderSlot?> LoadSlotAsync(Guid binderId, Guid slotId, CancellationToken ct) =>
        _db.BinderSlots
            .Include(s => s.CardVariant).ThenInclude(v => v!.Card).ThenInclude(c => c!.Set)
            .Include(s => s.CardVariant).ThenInclude(v => v!.VariantType)
            .Include(s => s.OverlayTag)
            .FirstOrDefaultAsync(s => s.Id == slotId && s.Page!.BinderId == binderId, ct);

    private async Task ReloadSlotDetailsAsync(BinderSlot slot, CancellationToken ct)
    {
        await _db.Entry(slot).Reference(s => s.CardVariant).LoadAsync(ct);
        if (slot.CardVariant is not null)
        {
            await _db.Entry(slot.CardVariant).Reference(v => v.Card).LoadAsync(ct);
            await _db.Entry(slot.CardVariant).Reference(v => v.VariantType).LoadAsync(ct);
            if (slot.CardVariant.Card is not null)
            {
                await _db.Entry(slot.CardVariant.Card).Reference(c => c.Set).LoadAsync(ct);
            }
        }

        await _db.Entry(slot).Reference(s => s.OverlayTag).LoadAsync(ct);
    }

    private static Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary BuildModelState(string key, string message)
    {
        var modelState = new Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary();
        modelState.AddModelError(key, message);
        return modelState;
    }
}
