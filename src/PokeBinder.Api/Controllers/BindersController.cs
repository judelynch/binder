using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PokeBinder.Api.Dtos;
using PokeBinder.Core.Binders;
using PokeBinder.Infrastructure;

namespace PokeBinder.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/binders")]
public class BindersController : ControllerBase
{
    private readonly PokeBinderDbContext _db;

    public BindersController(PokeBinderDbContext db)
    {
        _db = db;
    }

    [HttpPost]
    public async Task<ActionResult<BinderSummaryDto>> Create(CreateBinderRequest request, CancellationToken ct)
    {
        if (request.InitialPageCount % 2 != 0)
        {
            return ValidationProblem(BuildModelState(nameof(request.InitialPageCount), "Page count must be even."));
        }

        var binder = new Binder
        {
            Id = Guid.NewGuid(),
            OwnerId = this.GetUserId(),
            Name = request.Name,
            ColourHex = request.ColourHex,
            Rows = request.Rows,
            Columns = request.Columns,
            CreatedAt = DateTime.UtcNow
        };

        BinderPageFactory.AppendPages(_db, binder, startingPageNumber: 1, count: request.InitialPageCount);

        _db.Binders.Add(binder);
        await _db.SaveChangesAsync(ct);

        return Ok(ToSummary(binder));
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BinderSummaryDto>>> List(CancellationToken ct)
    {
        var userId = this.GetUserId();

        var binders = await _db.Binders
            .Where(b => b.OwnerId == userId)
            .OrderByDescending(b => b.CreatedAt)
            .Select(b => new BinderSummaryDto(
                b.Id, b.Name, b.ColourHex, b.Rows, b.Columns,
                b.Pages.Count,
                b.Pages.SelectMany(p => p.Slots).Count(),
                b.Pages.SelectMany(p => p.Slots).Count(s => s.CardVariantId != null),
                b.Pages.SelectMany(p => p.Slots).Count(s => s.Owned),
                b.Pages.SelectMany(p => p.Slots).Count(s => s.CardVariantId != null && !s.Owned),
                b.CreatedAt,
                b.LastAccessedAt))
            .ToListAsync(ct);

        return Ok(binders);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BinderSummaryDto>> GetById(Guid id, CancellationToken ct)
    {
        var userId = this.GetUserId();

        var binder = await _db.Binders
            .Where(b => b.Id == id && b.OwnerId == userId)
            .Select(b => new BinderSummaryDto(
                b.Id, b.Name, b.ColourHex, b.Rows, b.Columns,
                b.Pages.Count,
                b.Pages.SelectMany(p => p.Slots).Count(),
                b.Pages.SelectMany(p => p.Slots).Count(s => s.CardVariantId != null),
                b.Pages.SelectMany(p => p.Slots).Count(s => s.Owned),
                b.Pages.SelectMany(p => p.Slots).Count(s => s.CardVariantId != null && !s.Owned),
                b.CreatedAt,
                b.LastAccessedAt))
            .FirstOrDefaultAsync(ct);

        if (binder is null)
        {
            return NotFound();
        }

        return Ok(binder);
    }

    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<BinderSummaryDto>> Update(Guid id, UpdateBinderRequest request, CancellationToken ct)
    {
        var binder = await FindOwnedBinderAsync(id, ct, includePages: true);
        if (binder is null)
        {
            return NotFound();
        }

        if (request.Name is not null)
        {
            binder.Name = request.Name;
        }

        if (request.ColourHex is not null)
        {
            binder.ColourHex = request.ColourHex;
        }

        await _db.SaveChangesAsync(ct);
        return Ok(ToSummary(binder));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var binder = await FindOwnedBinderAsync(id, ct, includePages: false);
        if (binder is null)
        {
            return NotFound();
        }

        _db.Binders.Remove(binder);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/pages")]
    public async Task<ActionResult<BinderSummaryDto>> AppendPages(Guid id, AppendPagesRequest request, CancellationToken ct)
    {
        if (request.Count % 2 != 0)
        {
            return ValidationProblem(BuildModelState(nameof(request.Count), "Page count must be even."));
        }

        var binder = await FindOwnedBinderAsync(id, ct, includePages: true);
        if (binder is null)
        {
            return NotFound();
        }

        var nextPageNumber = binder.Pages.Count == 0 ? 1 : binder.Pages.Max(p => p.PageNumber) + 1;
        BinderPageFactory.AppendPages(_db, binder, nextPageNumber, request.Count);

        await _db.SaveChangesAsync(ct);
        return Ok(ToSummary(binder));
    }

    [HttpDelete("{id:guid}/pages/{pageNumber:int}")]
    public async Task<ActionResult<BinderSummaryDto>> DeleteSheet(Guid id, int pageNumber, [FromQuery] bool force, CancellationToken ct)
    {
        var binder = await FindOwnedBinderAsync(id, ct, includePages: true);
        if (binder is null)
        {
            return NotFound();
        }

        var (first, second) = SpreadCalculator.GetSheetPair(pageNumber);

        var pagesToDelete = binder.Pages.Where(p => p.PageNumber == first || p.PageNumber == second).ToList();
        if (pagesToDelete.Count == 0)
        {
            return NotFound();
        }

        var hasAssignedSlots = pagesToDelete.Any(p => p.Slots.Any(s => s.CardVariantId != null));
        if (hasAssignedSlots && !force)
        {
            return Conflict(new { message = $"Pages {first} and {second} have assigned slots. Pass ?force=true to delete them anyway." });
        }

        await using var transaction = await _db.Database.BeginTransactionAsync(ct);

        foreach (var page in pagesToDelete)
        {
            binder.Pages.Remove(page);
            _db.BinderPages.Remove(page);
        }

        foreach (var page in binder.Pages.Where(p => p.PageNumber > second))
        {
            page.PageNumber -= 2;
        }

        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return Ok(ToSummary(binder));
    }

    [HttpGet("{id:guid}/spread/{spreadIndex:int}")]
    public async Task<ActionResult<SpreadResponseDto>> GetSpread(Guid id, int spreadIndex, CancellationToken ct)
    {
        var userId = this.GetUserId();
        var binder = await _db.Binders
            .Where(b => b.Id == id && b.OwnerId == userId)
            .Select(b => new { b.Id, PageCount = b.Pages.Count })
            .FirstOrDefaultAsync(ct);

        if (binder is null)
        {
            return NotFound();
        }

        SpreadResult spread;
        try
        {
            spread = SpreadCalculator.GetSpread(binder.PageCount, spreadIndex);
        }
        catch (ArgumentOutOfRangeException)
        {
            return NotFound();
        }

        var leftPanel = await BuildPanelAsync(id, spread.Left, ct);
        var rightPanel = await BuildPanelAsync(id, spread.Right, ct);

        await _db.Binders
            .Where(b => b.Id == id)
            .ExecuteUpdateAsync(setters => setters.SetProperty(b => b.LastAccessedAt, DateTime.UtcNow), ct);

        return Ok(new SpreadResponseDto(leftPanel, rightPanel, spread.TotalSpreads));
    }

    private async Task<SpreadPanelDto> BuildPanelAsync(Guid binderId, SpreadPanel panel, CancellationToken ct)
    {
        if (panel.Type == SpreadPanelType.Cover)
        {
            return new SpreadPanelDto("cover", null, null);
        }

        var slots = await _db.BinderSlots
            .Where(s => s.Page!.BinderId == binderId && s.Page!.PageNumber == panel.PageNumber)
            .OrderBy(s => s.Position)
            .Select(s => new BinderSlotDto(
                s.Id,
                s.Position,
                s.CardVariant == null ? null : new CardSlotSummaryDto(
                    s.CardVariant.Card!.Id,
                    s.CardVariant.Card.Name,
                    s.CardVariant.Card.ImageSmallUrl,
                    s.CardVariant.Card.ImageLargeUrl,
                    s.CardVariant.Card.SetId,
                    s.CardVariant.Card.Set!.Name,
                    s.CardVariant.Card.Number,
                    s.CardVariant.Card.Rarity),
                s.CardVariant == null ? null : s.CardVariant.VariantType!.Name,
                s.Owned,
                s.Quantity,
                s.Condition == null ? null : s.Condition.ToString(),
                s.OverlayTag == null ? null : new OverlayTagDto(s.OverlayTag.Id, s.OverlayTag.Name, s.OverlayTag.ColourHex)))
            .ToListAsync(ct);

        return new SpreadPanelDto("page", panel.PageNumber, slots);
    }

    private async Task<Binder?> FindOwnedBinderAsync(Guid id, CancellationToken ct, bool includePages)
    {
        var userId = this.GetUserId();
        var query = _db.Binders.Where(b => b.Id == id && b.OwnerId == userId);

        if (includePages)
        {
            query = query.Include(b => b.Pages).ThenInclude(p => p.Slots);
        }

        return await query.FirstOrDefaultAsync(ct);
    }

    private static BinderSummaryDto ToSummary(Binder binder)
    {
        var slots = binder.Pages.SelectMany(p => p.Slots).ToList();
        var filled = slots.Count(s => s.CardVariantId != null);
        var owned = slots.Count(s => s.Owned);
        var missing = slots.Count(s => s.CardVariantId != null && !s.Owned);

        return new BinderSummaryDto(
            binder.Id, binder.Name, binder.ColourHex, binder.Rows, binder.Columns,
            binder.Pages.Count, slots.Count, filled, owned, missing,
            binder.CreatedAt, binder.LastAccessedAt);
    }

    private static Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary BuildModelState(string key, string message)
    {
        var modelState = new Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary();
        modelState.AddModelError(key, message);
        return modelState;
    }
}
