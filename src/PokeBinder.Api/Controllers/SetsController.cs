using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PokeBinder.Api.Dtos;
using PokeBinder.Api.Pricing;
using PokeBinder.Infrastructure;

namespace PokeBinder.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/sets")]
public class SetsController : ControllerBase
{
    private readonly PokeBinderDbContext _db;

    public SetsController(PokeBinderDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SetSummaryDto>>> GetSets(CancellationToken ct)
    {
        var userId = this.GetUserId();

        // A card counts toward OwnedCount once every "required" variant of it is owned - required
        // meaning every variant except ones whose type name contains "Stamp" (e.g. "Promo Stamp"),
        // which are deliberately excluded from set-completion. ToUpper().Contains(...) neutralizes
        // case on both sides before comparing, so this doesn't depend on the database's collation
        // being case-insensitive - relying on that implicitly would be a silent, easy-to-miss bug.
        // A card whose only variant is a Stamp variant has zero required variants, so it counts as
        // owned vacuously (no unmet requirement) - intentional, not an edge case to "fix".
        var sets = await _db.Sets
            .OrderByDescending(s => s.ReleaseDate)
            .Select(s => new SetSummaryDto(
                s.Id, s.Name, s.Series, s.PrintedTotal, s.Total, s.ReleaseDate,
                s.PtcgoCode, s.SymbolImageUrl, s.LogoImageUrl,
                s.Cards.Count(),
                s.Cards.Count(c => !c.Variants.Any(v =>
                    !v.VariantType!.Name.ToUpper().Contains("STAMP") &&
                    !_db.CardOwnerships.Any(o => o.CardVariantId == v.Id && o.UserId == userId && o.Quantity >= 1)))))
            .ToListAsync(ct);

        return Ok(sets);
    }

    [HttpGet("{id}/cards")]
    public async Task<ActionResult<PagedResult<CardSummaryDto>>> GetSetCards(
        string id, [FromQuery] string? name = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 500); // 500 comfortably covers the largest real set (swshp, 304 cards) for the master-set checklist

        var setExists = await _db.Sets.AnyAsync(s => s.Id == id, ct);
        if (!setExists)
        {
            return NotFound();
        }

        var query = _db.Cards.Where(c => c.SetId == id);

        if (!string.IsNullOrWhiteSpace(name))
        {
            query = query.Where(c => EF.Functions.Like(c.Name, $"%{name}%"));
        }

        query = query
            .OrderBy(c => c.NumberSortGroup)
            .ThenBy(c => c.NumberSortPrefix)
            .ThenBy(c => c.NumberSortValue)
            .ThenBy(c => c.NumberSortSuffix)
            .ThenBy(c => c.Number);

        var totalCount = await query.CountAsync(ct);
        var userId = this.GetUserId();

        var cards = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new
            {
                c.Id,
                c.SetId,
                c.Name,
                c.Number,
                c.Rarity,
                c.Supertype,
                c.ImageSmallUrl,
                c.ImageLargeUrl,
                Variants = c.Variants
                    .OrderBy(v => v.VariantType!.Name != "Normal").ThenBy(v => v.VariantType!.Name)
                    .Select(v => new { v.Id, VariantTypeName = v.VariantType!.Name })
                    .ToList(),
            })
            .ToListAsync(ct);

        // A second round trip for ownership (rather than a correlated subquery per variant, per field)
        // avoids both a nullable-enum-in-subquery EF translation risk and, more importantly, a real
        // bug: Nullable<T>.ToString() returns "" (not null) when the nullable has no value, which
        // would have silently turned "unowned" into Condition = "" instead of null.
        var variantIds = cards.SelectMany(c => c.Variants.Select(v => v.Id)).ToList();
        var ownershipByVariant = await _db.CardOwnerships
            .Where(o => o.UserId == userId && variantIds.Contains(o.CardVariantId))
            .ToDictionaryAsync(o => o.CardVariantId, ct);

        var items = cards
            .Select(c => new CardSummaryDto(
                c.Id, c.SetId, c.Name, c.Number, c.Rarity, c.Supertype, c.ImageSmallUrl, c.ImageLargeUrl,
                c.Variants.Select(v =>
                {
                    ownershipByVariant.TryGetValue(v.Id, out var ownership);
                    return new OwnedVariantSummaryDto(
                        v.Id, v.VariantTypeName, ownership is not null, ownership?.Quantity ?? 0, ownership?.Condition?.ToString());
                }).ToList()))
            .ToList();

        return Ok(new PagedResult<CardSummaryDto>(items, page, pageSize, totalCount));
    }

    /// <summary>Best-available price for every variant of every card in this set, for the tile badges on the set page. Only variants with actual price data are returned (unlike the single-card endpoint) - a full set can have hundreds of unpriced variants and there's no per-tile need to distinguish "not priced yet" from "not in this response".</summary>
    [HttpGet("{id}/prices")]
    public async Task<ActionResult<IReadOnlyList<CardVariantPriceDto>>> GetSetPrices(string id, CancellationToken ct)
    {
        var variantIds = await _db.CardVariants.Where(v => v.Card!.SetId == id).Select(v => v.Id).ToListAsync(ct);
        if (variantIds.Count == 0)
        {
            return Ok(Array.Empty<CardVariantPriceDto>());
        }

        var pricePoints = await _db.PricePoints
            .Where(p => variantIds.Contains(p.CardVariantId) && p.QuarantinedReason == null && p.GradedStatus == Core.Pricing.GradedStatus.Raw)
            .ToListAsync(ct);

        var result = pricePoints
            .GroupBy(p => p.CardVariantId)
            .Select(g => CardVariantPriceMapping.ToDto(g.Key, g.ToList(), null))
            .ToList();

        return Ok(result);
    }
}
