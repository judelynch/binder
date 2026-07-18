using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PokeBinder.Api.Dtos;
using PokeBinder.Core.Binders;
using PokeBinder.Core.Cards;
using PokeBinder.Core.Pricing;
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

    [HttpGet("{id:guid}/cards")]
    public async Task<ActionResult<IReadOnlyList<BinderCardRowDto>>> GetBinderCards(Guid id, CancellationToken ct)
    {
        var userId = this.GetUserId();
        var exists = await _db.Binders.AnyAsync(b => b.Id == id && b.OwnerId == userId, ct);
        if (!exists)
        {
            return NotFound();
        }

        var rows = await _db.BinderSlots
            .Where(s => s.Page!.BinderId == id && s.CardVariantId != null)
            .OrderBy(s => s.Page!.PageNumber).ThenBy(s => s.Position)
            .Select(s => new BinderCardRowDto(
                s.Id,
                s.Page!.PageNumber,
                s.Position,
                s.CardVariant!.Card!.Id,
                s.CardVariant.Card.Name,
                s.CardVariant.Card.SetId,
                s.CardVariant.Card.Set!.Name,
                s.CardVariant.Card.Number,
                s.CardVariant.Card.Set.ReleaseDate.Year,
                s.Owned,
                s.OverlayTag == null ? (Guid?)null : s.OverlayTag.Id,
                s.OverlayTag == null ? null : s.OverlayTag.Name,
                s.OverlayTag == null ? null : s.OverlayTag.ColourHex))
            .ToListAsync(ct);

        return Ok(rows);
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

    [HttpGet("{id:guid}/spread/{spreadIndex:int}/suggestions")]
    public async Task<ActionResult<IReadOnlyList<SlotSuggestionsDto>>> GetSuggestions(Guid id, int spreadIndex, CancellationToken ct)
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

        var relevantPages = new HashSet<int>();
        if (spread.Left.Type == SpreadPanelType.Page) relevantPages.Add(spread.Left.PageNumber!.Value);
        if (spread.Right.Type == SpreadPanelType.Page) relevantPages.Add(spread.Right.PageNumber!.Value);

        if (relevantPages.Count == 0)
        {
            return Ok(Array.Empty<SlotSuggestionsDto>());
        }

        var placedRaw = await _db.BinderSlots
            .Where(s => s.Page!.BinderId == id && s.CardVariantId != null)
            .Select(s => new
            {
                s.Id,
                PageNumber = s.Page!.PageNumber,
                CardId = s.CardVariant!.Card!.Id,
                s.CardVariant.Card.Name,
                s.CardVariant.Card.SetId,
                ReleaseDate = s.CardVariant.Card.Set!.ReleaseDate,
                s.CardVariant.Card.NumberSortGroup,
                s.CardVariant.Card.NumberSortPrefix,
                s.CardVariant.Card.NumberSortValue,
                s.CardVariant.Card.NumberSortSuffix,
                s.CardVariant.Card.Rarity,
                s.CardVariant.Card.Types,
            })
            .ToListAsync(ct);

        if (placedRaw.Count == 0)
        {
            return Ok(Array.Empty<SlotSuggestionsDto>());
        }

        var placedCards = placedRaw
            .Select(p => new PlacedCard(
                p.Id, p.CardId, p.Name, p.SetId, p.ReleaseDate,
                new SortKey(p.NumberSortGroup, p.NumberSortPrefix, p.NumberSortValue, p.NumberSortSuffix),
                p.Rarity, p.Types))
            .ToList();

        var normalVariantTypeId = await _db.VariantTypes.Where(v => v.Name == "Normal").Select(v => v.Id).SingleAsync(ct);

        var setIds = placedCards.Select(p => p.SetId).Distinct().ToList();
        var setCatalog = (await ProjectCatalogRows(_db.Cards.Where(c => setIds.Contains(c.SetId)), normalVariantTypeId).ToListAsync(ct))
            .Select(ToCatalogCard)
            .GroupBy(c => c.SetId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<CatalogCard>)g.ToList());

        var names = placedCards.Select(p => p.Name).Distinct().ToList();
        var nameCatalog = (await ProjectCatalogRows(_db.Cards.Where(c => names.Contains(c.Name)), normalVariantTypeId).ToListAsync(ct))
            .Select(ToCatalogCard)
            .GroupBy(c => c.Name)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<CatalogCard>)g.ToList());

        var themeKeys = placedCards
            .Where(p => p.Rarity != null)
            .SelectMany(p => p.Types.Select(t => new ThemeKey(p.Rarity!, t)))
            .Distinct()
            .ToList();

        var themeCatalog = new Dictionary<ThemeKey, IReadOnlyList<CatalogCard>>();
        if (themeKeys.Count > 0)
        {
            var rarities = themeKeys.Select(k => k.Rarity).Distinct().ToList();
            var types = themeKeys.Select(k => k.Type).Distinct().ToList();
            var themeCandidates = (await ProjectCatalogRows(
                    _db.Cards.Where(c => c.Rarity != null && rarities.Contains(c.Rarity) && c.TypeRows.Any(t => types.Contains(t.Type))),
                    normalVariantTypeId)
                .ToListAsync(ct))
                .Select(ToCatalogCard)
                .ToList();

            foreach (var key in themeKeys)
            {
                themeCatalog[key] = themeCandidates.Where(c => c.Rarity == key.Rarity && c.Types.Contains(key.Type)).ToList();
            }
        }

        var suggestionsBySlot = SuggestionEngine.ComputeSuggestions(placedCards, setCatalog, nameCatalog, themeCatalog);

        var slotPageLookup = placedRaw.ToDictionary(p => p.Id, p => p.PageNumber);
        var relevantSuggestions = suggestionsBySlot
            .Where(kv => relevantPages.Contains(slotPageLookup.GetValueOrDefault(kv.Key)))
            .ToList();

        if (relevantSuggestions.Count == 0)
        {
            return Ok(Array.Empty<SlotSuggestionsDto>());
        }

        var suggestedCardIds = relevantSuggestions.SelectMany(kv => kv.Value).Select(s => s.CardId).Distinct().ToList();
        var displayInfo = await _db.Cards
            .Where(c => suggestedCardIds.Contains(c.Id))
            .Select(c => new { c.Id, c.Number, c.ImageSmallUrl, c.SetId, c.Rarity, SetName = c.Set!.Name })
            .ToDictionaryAsync(c => c.Id, ct);

        var result = relevantSuggestions
            .Select(kv => new SlotSuggestionsDto(
                kv.Key,
                kv.Value.Select(s =>
                {
                    var display = displayInfo[s.CardId];
                    return new SuggestedCardDto(s.CardId, s.Name, s.SetId, display.SetName, display.Number, display.ImageSmallUrl, display.Rarity, s.DefaultVariantId, s.Reason.ToString());
                }).ToList()))
            .ToList();

        return Ok(result);
    }

    /// <summary>
    /// Best-available price per card-variant currently assigned to this binder (any page, not just
    /// the loaded spread), plus binder-wide owned-value/missing-cost totals. "Best-available" =
    /// the cheapest published raw bucket for that variant, per the pricing plan's architecture
    /// decision - used identically for both the totals here and a greyed slot's cost-to-buy badge.
    /// Slots whose variant has no price data yet simply don't contribute to the totals (rather than
    /// making the whole total null), since that's the common state before enough listings accumulate.
    /// </summary>
    [HttpGet("{id:guid}/prices")]
    public async Task<ActionResult<BinderPriceSummaryDto>> GetPrices(Guid id, CancellationToken ct)
    {
        var userId = this.GetUserId();
        var binderExists = await _db.Binders.AnyAsync(b => b.Id == id && b.OwnerId == userId, ct);
        if (!binderExists)
        {
            return NotFound();
        }

        var slots = await _db.BinderSlots
            .Where(s => s.Page!.BinderId == id && s.CardVariantId != null)
            .Select(s => new { CardVariantId = s.CardVariantId!.Value, s.Owned, s.Quantity })
            .ToListAsync(ct);

        var variantIds = slots.Select(s => s.CardVariantId).Distinct().ToList();
        if (variantIds.Count == 0)
        {
            return Ok(new BinderPriceSummaryDto(null, null, Array.Empty<CardVariantPriceDto>()));
        }

        var pricePoints = await _db.PricePoints
            .Where(p => variantIds.Contains(p.CardVariantId) && p.QuarantinedReason == null)
            .ToListAsync(ct);
        var scrapedAtByVariant = await _db.CardVariantScrapeStates
            .Where(s => variantIds.Contains(s.CardVariantId))
            .ToDictionaryAsync(s => s.CardVariantId, s => s.LastScrapedAt, ct);

        var prices = pricePoints
            .GroupBy(p => p.CardVariantId)
            .Select(g => ToCardVariantPriceDto(g.Key, g.ToList(), scrapedAtByVariant.GetValueOrDefault(g.Key)))
            .ToList();
        var bestAvailableByVariant = prices.ToDictionary(p => p.CardVariantId, p => p.BestAvailableItemOnlyGbp);

        decimal? ownedValue = null;
        decimal? missingCost = null;
        foreach (var slot in slots)
        {
            if (!bestAvailableByVariant.TryGetValue(slot.CardVariantId, out var price) || price is null)
            {
                continue;
            }

            if (slot.Owned)
            {
                ownedValue = (ownedValue ?? 0m) + price.Value * (slot.Quantity ?? 1);
            }
            else
            {
                missingCost = (missingCost ?? 0m) + price.Value;
            }
        }

        return Ok(new BinderPriceSummaryDto(ownedValue, missingCost, prices));
    }

    private static CardVariantPriceDto ToCardVariantPriceDto(Guid cardVariantId, List<PricePoint> points, DateTime? lastScrapedAt)
    {
        PriceBucketDto ToBucket(PricePoint p) => new(
            p.GradedStatus.ToString(), p.Grader, p.Grade, p.Condition?.ToString(),
            p.WindowDays, p.ItemOnlyMedianGbp, p.DeliveredMedianGbp, p.SampleCount, p.LastSaleDate);

        var rawPoints = points.Where(p => p.GradedStatus == GradedStatus.Raw).ToList();
        var rawBuckets = rawPoints.OrderBy(p => p.Condition).ThenBy(p => p.WindowDays).Select(ToBucket).ToList();
        var gradedBuckets = points.Where(p => p.GradedStatus == GradedStatus.Graded)
            .OrderBy(p => p.Grader).ThenByDescending(p => p.Grade).ThenBy(p => p.WindowDays)
            .Select(ToBucket).ToList();

        var cheapestRaw = rawPoints.OrderBy(p => p.ItemOnlyMedianGbp).FirstOrDefault();

        return new CardVariantPriceDto(
            cardVariantId,
            cheapestRaw?.ItemOnlyMedianGbp,
            cheapestRaw?.DeliveredMedianGbp,
            rawBuckets,
            gradedBuckets,
            lastScrapedAt);
    }

    private record CardCatalogRow(
        string Id, string Name, string SetId, DateOnly ReleaseDate,
        byte NumberSortGroup, string NumberSortPrefix, int NumberSortValue, string NumberSortSuffix,
        string? Rarity, IReadOnlyList<string> Types, Guid DefaultVariantId);

    private static IQueryable<CardCatalogRow> ProjectCatalogRows(IQueryable<Card> query, Guid normalVariantTypeId) =>
        query.Select(c => new CardCatalogRow(
            c.Id, c.Name, c.SetId, c.Set!.ReleaseDate,
            c.NumberSortGroup, c.NumberSortPrefix, c.NumberSortValue, c.NumberSortSuffix,
            c.Rarity, c.Types,
            c.Variants.Where(v => v.VariantTypeId == normalVariantTypeId).Select(v => v.Id).FirstOrDefault()));

    private static CatalogCard ToCatalogCard(CardCatalogRow r) =>
        new(r.Id, r.Name, r.SetId, r.ReleaseDate, new SortKey(r.NumberSortGroup, r.NumberSortPrefix, r.NumberSortValue, r.NumberSortSuffix), r.Rarity, r.Types, r.DefaultVariantId);

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
                s.CardVariantId,
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
