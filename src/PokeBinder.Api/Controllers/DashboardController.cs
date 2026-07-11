using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PokeBinder.Api.Dtos;
using PokeBinder.Infrastructure;

namespace PokeBinder.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/dashboard")]
public class DashboardController : ControllerBase
{
    private readonly PokeBinderDbContext _db;

    public DashboardController(PokeBinderDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<DashboardResponseDto>> Get(CancellationToken ct)
    {
        var userId = this.GetUserId();

        var slots = _db.BinderSlots.Where(s => s.Page!.Binder!.OwnerId == userId);

        var cardsOwned = await slots.Where(s => s.Owned).SumAsync(s => s.Quantity ?? 1, ct);
        var cardsMissing = await slots.CountAsync(s => s.CardVariantId != null && !s.Owned, ct);
        var binderCount = await _db.Binders.CountAsync(b => b.OwnerId == userId, ct);

        var recentBinders = await _db.Binders
            .Where(b => b.OwnerId == userId)
            .OrderByDescending(b => b.LastAccessedAt)
            .Take(5)
            .Select(b => new
            {
                b.Id,
                b.Name,
                b.ColourHex,
                b.LastAccessedAt,
                Assigned = b.Pages.SelectMany(p => p.Slots).Count(s => s.CardVariantId != null),
                Owned = b.Pages.SelectMany(p => p.Slots).Count(s => s.Owned)
            })
            .ToListAsync(ct);

        var recentBinderDtos = recentBinders
            .Select(b => new DashboardBinderDto(
                b.Id, b.Name, b.ColourHex,
                b.Assigned == 0 ? 0 : Math.Round(b.Owned / (double)b.Assigned * 100, 1),
                b.LastAccessedAt))
            .ToList();

        return Ok(new DashboardResponseDto(cardsOwned, cardsMissing, binderCount, recentBinderDtos));
    }
}
