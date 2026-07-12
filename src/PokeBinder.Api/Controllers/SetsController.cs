using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PokeBinder.Api.Dtos;
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
        var sets = await _db.Sets
            .OrderByDescending(s => s.ReleaseDate)
            .Select(s => new SetSummaryDto(
                s.Id, s.Name, s.Series, s.PrintedTotal, s.Total, s.ReleaseDate,
                s.PtcgoCode, s.SymbolImageUrl, s.LogoImageUrl))
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

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CardSummaryDto(
                c.Id, c.SetId, c.Name, c.Number, c.Rarity, c.Supertype, c.ImageSmallUrl, c.ImageLargeUrl,
                c.Variants.Select(v => new VariantSummaryDto(v.Id, v.VariantType!.Name)).ToList()))
            .ToListAsync(ct);

        return Ok(new PagedResult<CardSummaryDto>(items, page, pageSize, totalCount));
    }
}
