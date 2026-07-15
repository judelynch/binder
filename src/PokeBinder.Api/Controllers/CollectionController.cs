using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PokeBinder.Api.Dtos;
using PokeBinder.Core.Binders;
using PokeBinder.Core.Collection;
using PokeBinder.Infrastructure;

namespace PokeBinder.Api.Controllers;

/// <summary>
/// A user's global "I own this card variant" fact, independent of whether (or where) that
/// variant sits in any binder. See CardOwnership - BinderSlot.Owned is a separate concept
/// and is never touched here.
/// </summary>
[ApiController]
[Authorize]
[Route("api/collection")]
public class CollectionController : ControllerBase
{
    private readonly PokeBinderDbContext _db;

    public CollectionController(PokeBinderDbContext db)
    {
        _db = db;
    }

    [HttpPut("ownership/{cardVariantId:guid}")]
    public async Task<ActionResult<CardOwnershipDto>> SetOwnership(Guid cardVariantId, SetOwnershipRequest request, CancellationToken ct)
    {
        if (request.Quantity < 1)
        {
            return ValidationProblem(BuildModelState(nameof(request.Quantity), "Quantity must be at least 1."));
        }

        CardCondition? condition = null;
        if (request.Condition is not null)
        {
            if (!Enum.TryParse<CardCondition>(request.Condition, out var parsed))
            {
                return ValidationProblem(BuildModelState(nameof(request.Condition), "Condition must be one of NM, LP, MP, HP, DMG."));
            }

            condition = parsed;
        }

        var variantExists = await _db.CardVariants.AnyAsync(v => v.Id == cardVariantId, ct);
        if (!variantExists)
        {
            return NotFound();
        }

        var userId = this.GetUserId();
        var ownership = await _db.CardOwnerships.FirstOrDefaultAsync(o => o.UserId == userId && o.CardVariantId == cardVariantId, ct);
        if (ownership is null)
        {
            ownership = new CardOwnership { Id = Guid.NewGuid(), UserId = userId, CardVariantId = cardVariantId };
            _db.CardOwnerships.Add(ownership);
        }

        ownership.Quantity = request.Quantity;
        ownership.Condition = condition;
        ownership.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return Ok(new CardOwnershipDto(cardVariantId, true, ownership.Quantity, ownership.Condition?.ToString()));
    }

    /// <summary>Idempotent - always 204, whether or not the variant was owned.</summary>
    [HttpDelete("ownership/{cardVariantId:guid}")]
    public async Task<IActionResult> UnsetOwnership(Guid cardVariantId, CancellationToken ct)
    {
        var userId = this.GetUserId();
        var ownership = await _db.CardOwnerships.FirstOrDefaultAsync(o => o.UserId == userId && o.CardVariantId == cardVariantId, ct);
        if (ownership is not null)
        {
            _db.CardOwnerships.Remove(ownership);
            await _db.SaveChangesAsync(ct);
        }

        return NoContent();
    }

    /// <summary>
    /// Marks (or unmarks) many variants owned in one round trip, for the set-detail page's
    /// select-then-bulk-mark flow ("select all", then "mark as owned"). Quantity is fixed at 1
    /// for newly-created rows - an already-owned variant is left untouched rather than having its
    /// quantity/condition clobbered by a bulk re-mark.
    /// </summary>
    [HttpPost("ownership/bulk")]
    public async Task<ActionResult<BulkOwnershipResultDto>> BulkSetOwnership(BulkSetOwnershipRequest request, CancellationToken ct)
    {
        var variantIds = request.CardVariantIds.Distinct().ToList();
        var validCount = await _db.CardVariants.CountAsync(v => variantIds.Contains(v.Id), ct);
        if (validCount != variantIds.Count)
        {
            return ValidationProblem(BuildModelState(nameof(request.CardVariantIds), "One or more card variants do not exist."));
        }

        var userId = this.GetUserId();
        var existing = await _db.CardOwnerships
            .Where(o => o.UserId == userId && variantIds.Contains(o.CardVariantId))
            .ToListAsync(ct);

        var changed = 0;
        if (request.Owned)
        {
            var alreadyOwned = existing.Select(o => o.CardVariantId).ToHashSet();
            foreach (var variantId in variantIds.Where(id => !alreadyOwned.Contains(id)))
            {
                _db.CardOwnerships.Add(new CardOwnership { Id = Guid.NewGuid(), UserId = userId, CardVariantId = variantId, Quantity = 1 });
                changed++;
            }
        }
        else
        {
            _db.CardOwnerships.RemoveRange(existing);
            changed = existing.Count;
        }

        await _db.SaveChangesAsync(ct);
        return Ok(new BulkOwnershipResultDto(changed));
    }

    private static Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary BuildModelState(string key, string message)
    {
        var modelState = new Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary();
        modelState.AddModelError(key, message);
        return modelState;
    }
}
