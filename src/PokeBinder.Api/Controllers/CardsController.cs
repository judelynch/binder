using System.Linq.Expressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PokeBinder.Api.Cards;
using PokeBinder.Api.Dtos;
using PokeBinder.Core.Cards;
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
}
