using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PokeBinder.Api.Dtos;
using PokeBinder.Core.Pricing;
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

        var (portfolioValue, topValuableCards) = await BuildPortfolioValueAsync(slots, ct);

        return Ok(new DashboardResponseDto(cardsOwned, cardsMissing, binderCount, recentBinderDtos, portfolioValue, topValuableCards));
    }

    /// <summary>
    /// Portfolio value = sum of best-available raw price (cheapest published raw bucket) across
    /// this user's owned binder slots, same rule as a single binder's owned-value total. Top 5
    /// valuable cards are ranked by per-unit price, not multiplied by quantity - "your five most
    /// valuable cards" means five distinct cards, not five copies of one.
    /// </summary>
    private async Task<(decimal? PortfolioValue, IReadOnlyList<DashboardValuableCardDto> TopValuableCards)> BuildPortfolioValueAsync(
        IQueryable<Core.Binders.BinderSlot> slots, CancellationToken ct)
    {
        var ownedSlots = await slots
            .Where(s => s.Owned && s.CardVariantId != null)
            .Select(s => new { CardVariantId = s.CardVariantId!.Value, s.Quantity })
            .ToListAsync(ct);

        var ownedVariantIds = ownedSlots.Select(s => s.CardVariantId).Distinct().ToList();
        if (ownedVariantIds.Count == 0)
        {
            return (null, Array.Empty<DashboardValuableCardDto>());
        }

        var bestPriceByVariant = (await _db.PricePoints
                .Where(p => ownedVariantIds.Contains(p.CardVariantId) && p.GradedStatus == GradedStatus.Raw && p.QuarantinedReason == null)
                .ToListAsync(ct))
            .GroupBy(p => p.CardVariantId)
            .ToDictionary(g => g.Key, g => g.OrderBy(p => p.ItemOnlyMedianGbp).First().ItemOnlyMedianGbp);

        if (bestPriceByVariant.Count == 0)
        {
            return (null, Array.Empty<DashboardValuableCardDto>());
        }

        decimal? portfolioValue = null;
        foreach (var slot in ownedSlots)
        {
            if (bestPriceByVariant.TryGetValue(slot.CardVariantId, out var price))
            {
                portfolioValue = (portfolioValue ?? 0m) + price * (slot.Quantity ?? 1);
            }
        }

        var topVariantIds = bestPriceByVariant.OrderByDescending(kv => kv.Value).Take(5).Select(kv => kv.Key).ToList();
        var cardInfoByVariant = await _db.CardVariants
            .Where(v => topVariantIds.Contains(v.Id))
            .Select(v => new
            {
                CardVariantId = v.Id,
                CardId = v.Card!.Id,
                v.Card.Name,
                v.Card.ImageSmallUrl,
                SetName = v.Card.Set!.Name,
                v.Card.Number,
                VariantTypeName = v.VariantType!.Name,
            })
            .ToDictionaryAsync(v => v.CardVariantId, ct);

        var topValuableCards = topVariantIds
            .Where(cardInfoByVariant.ContainsKey)
            .Select(id =>
            {
                var c = cardInfoByVariant[id];
                return new DashboardValuableCardDto(id, c.CardId, c.Name, c.ImageSmallUrl, c.SetName, c.Number, c.VariantTypeName, bestPriceByVariant[id]);
            })
            .ToList();

        return (portfolioValue, topValuableCards);
    }
}
