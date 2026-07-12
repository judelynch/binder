using System.Linq.Expressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

    [HttpGet("search")]
    public async Task<ActionResult<PagedResult<CardSearchResultDto>>> Search([FromQuery] CardSearchRequest request, CancellationToken ct)
    {
        var page = Math.Max(request.Page, 1);
        var pageSize = Math.Clamp(request.PageSize, 1, 500); // 500 = the select-all-results cap

        var query = _db.Cards.AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            query = query.Where(c => EF.Functions.Like(c.Name, $"%{request.Name}%"));
        }

        if (!string.IsNullOrWhiteSpace(request.Supertype))
        {
            query = query.Where(c => c.Supertype == request.Supertype);
        }

        if (request.Subtypes is { Length: > 0 })
        {
            query = query.Where(c => c.SubtypeRows.Any(s => request.Subtypes.Contains(s.Subtype)));
        }

        if (request.Types is { Length: > 0 })
        {
            query = query.Where(c => c.TypeRows.Any(t => request.Types.Contains(t.Type)));
        }

        if (request.SetIds is { Length: > 0 })
        {
            query = query.Where(c => request.SetIds.Contains(c.SetId));
        }

        if (request.Series is { Length: > 0 })
        {
            query = query.Where(c => request.Series.Contains(c.Set!.Series));
        }

        if (request.Rarities is { Length: > 0 })
        {
            query = query.Where(c => c.Rarity != null && request.Rarities.Contains(c.Rarity));
        }

        if (request.HpMin.HasValue)
        {
            query = query.Where(c => c.HpValue != null && c.HpValue >= request.HpMin);
        }

        if (request.HpMax.HasValue)
        {
            query = query.Where(c => c.HpValue != null && c.HpValue <= request.HpMax);
        }

        if (!string.IsNullOrWhiteSpace(request.WeaknessType))
        {
            query = query.Where(c => c.WeaknessTypeRows.Any(w => w.Type == request.WeaknessType));
        }

        if (!string.IsNullOrWhiteSpace(request.ResistanceType))
        {
            query = query.Where(c => c.ResistanceTypeRows.Any(r => r.Type == request.ResistanceType));
        }

        if (request.RetreatCostMin.HasValue)
        {
            query = query.Where(c => c.ConvertedRetreatCost != null && c.ConvertedRetreatCost >= request.RetreatCostMin);
        }

        if (request.RetreatCostMax.HasValue)
        {
            query = query.Where(c => c.ConvertedRetreatCost != null && c.ConvertedRetreatCost <= request.RetreatCostMax);
        }

        if (!string.IsNullOrWhiteSpace(request.Artist))
        {
            query = query.Where(c => c.Artist != null && EF.Functions.Like(c.Artist, $"%{request.Artist}%"));
        }

        if (request.RegulationMarks is { Length: > 0 })
        {
            query = query.Where(c => c.RegulationMark != null && request.RegulationMarks.Contains(c.RegulationMark));
        }

        if (request.NationalPokedexNumber.HasValue)
        {
            query = query.Where(c => c.PokedexNumbers.Any(p => p.Number == request.NationalPokedexNumber));
        }

        query = request.Sort switch
        {
            "name" => query.OrderBy(c => c.Name).ThenBy(c => c.Id),
            "releaseDate" => query
                .OrderByDescending(c => c.Set!.ReleaseDate)
                .ThenBy(c => c.NumberSortGroup).ThenBy(c => c.NumberSortPrefix).ThenBy(c => c.NumberSortValue).ThenBy(c => c.NumberSortSuffix),
            "rarity" => query.OrderByDescending(RarityRankExpression).ThenBy(c => c.Name),
            _ => query
                .OrderByDescending(c => c.Set!.ReleaseDate)
                .ThenBy(c => c.NumberSortGroup).ThenBy(c => c.NumberSortPrefix).ThenBy(c => c.NumberSortValue).ThenBy(c => c.NumberSortSuffix),
        };

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CardSearchResultDto(
                c.Id, c.SetId, c.Set!.Name, c.Name, c.Number, c.Rarity, c.Supertype, c.ImageSmallUrl, c.ImageLargeUrl,
                c.Variants.Select(v => new VariantSummaryDto(v.Id, v.VariantType!.Name)).ToList()))
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

        var dto = new CardDetailDto(
            card.Id, card.SetId, card.Name, card.Supertype, card.Subtypes, card.Level, card.Hp,
            card.Types, card.EvolvesFrom, card.Number, card.Artist, card.Rarity, card.FlavorText,
            card.RegulationMark,
            card.PokedexNumbers.Select(p => p.Number).OrderBy(n => n).ToList(),
            card.ImageSmallUrl, card.ImageLargeUrl,
            card.Variants.Select(v => v.VariantType!.Name).OrderBy(n => n).ToList());

        return Ok(dto);
    }
}
