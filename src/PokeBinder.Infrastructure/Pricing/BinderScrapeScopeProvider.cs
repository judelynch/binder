using Microsoft.EntityFrameworkCore;
using PokeBinder.Core.Pricing;

namespace PokeBinder.Infrastructure.Pricing;

public class BinderScrapeScopeProvider : IScrapeScopeProvider
{
    private readonly PokeBinderDbContext _db;

    public BinderScrapeScopeProvider(PokeBinderDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Guid>> GetInScopeCardVariantIdsAsync(CancellationToken ct) =>
        await _db.BinderSlots
            .Where(s => s.CardVariantId != null)
            .Select(s => s.CardVariantId!.Value)
            .Distinct()
            .ToListAsync(ct);
}
