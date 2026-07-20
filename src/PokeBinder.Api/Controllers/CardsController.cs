using System.Linq.Expressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PokeBinder.Api.Cards;
using PokeBinder.Api.Dtos;
using PokeBinder.Api.Pricing;
using PokeBinder.Core.Cards;
using PokeBinder.Core.Pricing;
using PokeBinder.Infrastructure;

namespace PokeBinder.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/cards")]
public class CardsController : ControllerBase
{
    private readonly PokeBinderDbContext _db;

    public CardsController(PokeBinderDbContext db)
    {
        _db = db;
    }

    /// <summary>Every user (not just admins) needs this to populate the variant filter in search.</summary>
    [HttpGet("variant-types")]
    public async Task<ActionResult<IReadOnlyList<string>>> GetVariantTypeNames(CancellationToken ct) =>
        Ok(await _db.VariantTypes.OrderBy(v => v.Name).Select(v => v.Name).ToListAsync(ct));

    [HttpGet("search")]
    public async Task<ActionResult<PagedResult<CardSearchResultDto>>> Search([FromQuery] CardSearchRequest request, CancellationToken ct)
    {
        var page = Math.Max(request.Page, 1);
        var pageSize = Math.Clamp(request.PageSize, 1, 500); // 500 = the select-all-results cap

        var query = _db.Cards.AsQueryable().ApplyFilters(request);

        // Price filtering isn't part of the shared ApplyFilters (that's also used by admin bulk-assign,
        // which has no reason to care about it) - a card matches on its BEST-AVAILABLE price (the
        // cheapest published raw bucket across its variants), the same "best available" rule used
        // everywhere else in the pricing UI, not "any bucket happens to fall in range".
        if (request.HasPriceData == true || request.PriceMin.HasValue || request.PriceMax.HasValue)
        {
            var matchingVariantIds = await _db.PricePoints
                .Where(p => p.GradedStatus == GradedStatus.Raw && p.QuarantinedReason == null)
                .GroupBy(p => p.CardVariantId)
                .Select(g => new { CardVariantId = g.Key, BestPrice = g.Min(p => p.ItemOnlyMedianGbp) })
                .Where(x => !request.PriceMin.HasValue || x.BestPrice >= request.PriceMin)
                .Where(x => !request.PriceMax.HasValue || x.BestPrice <= request.PriceMax)
                .Select(x => x.CardVariantId)
                .ToListAsync(ct);

            query = query.Where(c => c.Variants.Any(v => matchingVariantIds.Contains(v.Id)));
        }

        query = request.Sort switch
        {
            "name" => (request.SortDescending ?? false)
                ? query.OrderByDescending(c => c.Name).ThenBy(c => c.Id)
                : query.OrderBy(c => c.Name).ThenBy(c => c.Id),
            "releaseDate" => (request.SortDescending ?? true)
                ? query.OrderByDescending(c => c.Set!.ReleaseDate)
                    .ThenBy(c => c.NumberSortGroup).ThenBy(c => c.NumberSortPrefix).ThenBy(c => c.NumberSortValue).ThenBy(c => c.NumberSortSuffix)
                : query.OrderBy(c => c.Set!.ReleaseDate)
                    .ThenBy(c => c.NumberSortGroup).ThenBy(c => c.NumberSortPrefix).ThenBy(c => c.NumberSortValue).ThenBy(c => c.NumberSortSuffix),
            "rarity" => (request.SortDescending ?? true)
                ? query.OrderByDescending(RarityRankExpression).ThenBy(c => c.Name)
                : query.OrderBy(RarityRankExpression).ThenBy(c => c.Name),
            _ => (request.SortDescending ?? true)
                ? query.OrderByDescending(c => c.Set!.ReleaseDate)
                    .ThenBy(c => c.NumberSortGroup).ThenBy(c => c.NumberSortPrefix).ThenBy(c => c.NumberSortValue).ThenBy(c => c.NumberSortSuffix)
                : query.OrderBy(c => c.Set!.ReleaseDate)
                    .ThenBy(c => c.NumberSortGroup).ThenBy(c => c.NumberSortPrefix).ThenBy(c => c.NumberSortValue).ThenBy(c => c.NumberSortSuffix),
        };

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CardSearchResultDto(
                c.Id, c.SetId, c.Set!.Name, c.Name, c.Number, c.Rarity, c.Supertype, c.ImageSmallUrl, c.ImageLargeUrl,
                c.Variants
                    .OrderBy(v => v.VariantType!.Name != "Normal").ThenBy(v => v.VariantType!.Name)
                    .Select(v => new VariantSummaryDto(v.Id, v.VariantType!.Name)).ToList()))
            .ToListAsync(ct);

        return Ok(new PagedResult<CardSearchResultDto>(items, page, pageSize, totalCount));
    }

    /// <summary>
    /// Best-effort rarity ranking (common -> rarest) for the "rarity" sort option. Must be a real
    /// Expression&lt;Func&lt;...&gt;&gt; (not a plain method called from the query) — EF Core can only
    /// translate the expression tree it's actually given; a call to a separate compiled method
    /// fails with "could not be translated" because EF never sees inside the method body.
    /// </summary>
    private static readonly Expression<Func<Card, int>> RarityRankExpression = c =>
        c.Rarity == "Common" ? 1 :
        c.Rarity == "Uncommon" ? 2 :
        c.Rarity == "Rare" ? 3 :
        c.Rarity == "Rare ACE" ? 4 :
        c.Rarity == "ACE SPEC Rare" ? 5 :
        c.Rarity == "Promo" ? 6 :
        c.Rarity == "Rare Holo" ? 7 :
        c.Rarity == "Rare BREAK" ? 8 :
        c.Rarity == "Rare Prime" ? 9 :
        c.Rarity == "Rare Shining" ? 10 :
        c.Rarity == "Rare Shiny" ? 11 :
        c.Rarity == "LEGEND" ? 12 :
        c.Rarity == "Rare Holo Star" ? 13 :
        c.Rarity == "Rare Prism Star" ? 14 :
        c.Rarity == "Radiant Rare" ? 15 :
        c.Rarity == "Amazing Rare" ? 16 :
        c.Rarity == "Rare Holo EX" ? 17 :
        c.Rarity == "Rare Holo GX" ? 18 :
        c.Rarity == "Rare Holo LV.X" ? 19 :
        c.Rarity == "Rare Holo V" ? 20 :
        c.Rarity == "Rare Holo VMAX" ? 21 :
        c.Rarity == "Rare Holo VSTAR" ? 22 :
        c.Rarity == "Rare Shiny GX" ? 23 :
        c.Rarity == "Double Rare" ? 24 :
        c.Rarity == "Ultra Rare" ? 25 :
        c.Rarity == "Shiny Ultra Rare" ? 26 :
        c.Rarity == "Rare Rainbow" ? 27 :
        c.Rarity == "Rare Secret" ? 28 :
        c.Rarity == "Rare Ultra" ? 29 :
        c.Rarity == "Illustration Rare" ? 30 :
        c.Rarity == "Trainer Gallery Rare Holo" ? 31 :
        c.Rarity == "Classic Collection" ? 32 :
        c.Rarity == "Hyper Rare" ? 33 :
        c.Rarity == "Special Illustration Rare" ? 34 :
        c.Rarity == "Mega Hyper Rare" ? 35 :
        c.Rarity == "MEGA_ATTACK_RARE" ? 36 :
        c.Rarity == "Black White Rare" ? 37 :
        c.Rarity == "Shiny Rare" ? 38 :
        0;

    [HttpGet("{externalId}")]
    public async Task<ActionResult<CardDetailDto>> GetCard(string externalId, CancellationToken ct)
    {
        var card = await _db.Cards
            .Include(c => c.PokedexNumbers)
            .Include(c => c.Variants).ThenInclude(v => v.VariantType)
            .FirstOrDefaultAsync(c => c.Id == externalId, ct);

        if (card is null)
        {
            return NotFound();
        }

        var userId = this.GetUserId();
        var variantIds = card.Variants.Select(v => v.Id).ToList();
        var ownershipByVariant = await _db.CardOwnerships
            .Where(o => o.UserId == userId && variantIds.Contains(o.CardVariantId))
            .ToDictionaryAsync(o => o.CardVariantId, ct);

        var variants = card.Variants
            .OrderBy(v => v.VariantType!.Name != "Normal").ThenBy(v => v.VariantType!.Name)
            .Select(v =>
            {
                ownershipByVariant.TryGetValue(v.Id, out var ownership);
                return new OwnedVariantSummaryDto(
                    v.Id, v.VariantType!.Name, ownership is not null, ownership?.Quantity ?? 0, ownership?.Condition?.ToString());
            })
            .ToList();

        var dto = new CardDetailDto(
            card.Id, card.SetId, card.Name, card.Supertype, card.Subtypes, card.Level, card.Hp,
            card.Types, card.EvolvesFrom,
            card.Abilities, card.Attacks, card.Weaknesses, card.Resistances,
            card.RetreatCost, card.ConvertedRetreatCost,
            card.Number, card.Artist, card.Rarity, card.FlavorText,
            card.RegulationMark,
            card.PokedexNumbers.Select(p => p.Number).OrderBy(n => n).ToList(),
            card.ImageSmallUrl, card.ImageLargeUrl,
            variants);

        return Ok(dto);
    }

    /// <summary>Best-available price + raw/graded buckets for every variant of this card, one entry per variant regardless of whether it has price data yet.</summary>
    [HttpGet("{externalId}/prices")]
    public async Task<ActionResult<IReadOnlyList<CardVariantPriceDto>>> GetCardPrices(string externalId, CancellationToken ct)
    {
        var variantIds = await _db.CardVariants.Where(v => v.CardId == externalId).Select(v => v.Id).ToListAsync(ct);
        if (variantIds.Count == 0)
        {
            return NotFound();
        }

        var pricePoints = await _db.PricePoints
            .Where(p => variantIds.Contains(p.CardVariantId) && p.QuarantinedReason == null)
            .ToListAsync(ct);
        var scrapedAtByVariant = await _db.CardVariantScrapeStates
            .Where(s => variantIds.Contains(s.CardVariantId))
            .ToDictionaryAsync(s => s.CardVariantId, s => s.LastScrapedAt, ct);

        var byVariant = pricePoints.GroupBy(p => p.CardVariantId).ToDictionary(g => g.Key, g => g.ToList());

        var result = variantIds
            .Select(id => CardVariantPriceMapping.ToDto(id, byVariant.GetValueOrDefault(id) ?? new List<PricePoint>(), scrapedAtByVariant.GetValueOrDefault(id)))
            .ToList();

        return Ok(result);
    }

    /// <summary>
    /// Every individual accepted sale for one variant, oldest first, with everything captured about
    /// each listing - backs both the card detail page's trend chart and its full sale-history list.
    /// Same eligibility rule as the aggregator (AutoAccepted, not a best-offer sale, English) so this
    /// only ever shows sales that actually counted toward a price.
    /// </summary>
    [HttpGet("{externalId}/variants/{variantId:guid}/price-history")]
    public async Task<ActionResult<IReadOnlyList<PriceHistoryPointDto>>> GetVariantPriceHistory(string externalId, Guid variantId, CancellationToken ct)
    {
        var variantExists = await _db.CardVariants.AnyAsync(v => v.Id == variantId && v.CardId == externalId, ct);
        if (!variantExists)
        {
            return NotFound();
        }

        var history = await _db.ListingClassifications
            .Where(c => c.ResolvedCardVariantId == variantId
                && c.Status == ClassificationStatus.AutoAccepted
                && !c.BestOfferAccepted
                && c.Language == "English")
            .OrderBy(c => c.RawListing!.SoldDate)
            .Select(c => new PriceHistoryPointDto(
                c.RawListing!.SoldDate,
                c.RawListing.Title,
                c.RawListing.ItemPriceGbp,
                c.RawListing.PostagePriceGbp,
                c.RawListing.ItemPriceGbp + (c.RawListing.PostagePriceGbp ?? 0m),
                c.RawListing.ListingFormat.ToString(),
                c.RawListing.ThumbnailUrl,
                c.GradedStatus.ToString(),
                c.Grader,
                c.Grade,
                c.RawCondition.ToString()))
            .ToListAsync(ct);

        return Ok(history);
    }
}
