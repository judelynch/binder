using Microsoft.EntityFrameworkCore;
using PokeBinder.Api.Dtos;
using PokeBinder.Core.Cards;

namespace PokeBinder.Api.Cards;

/// <summary>
/// The combinable card filters shared by card search (Phase 5) and admin bulk-variant assignment
/// (Phase 6), extracted so both callers build the exact same population from the exact same rules.
/// Sort/Page/PageSize on <see cref="CardSearchRequest"/> are intentionally not applied here — callers
/// decide their own ordering/pagination on top of the filtered query.
/// </summary>
public static class CardFilterExtensions
{
    public static IQueryable<Card> ApplyFilters(this IQueryable<Card> query, CardSearchRequest request)
    {
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

        return query;
    }
}
