using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PokeBinder.Api.Dtos;
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
