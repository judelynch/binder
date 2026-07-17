namespace PokeBinder.Core.Pricing;

/// <summary>
/// Which CardVariants the pricing pipeline should track. This phase ships binder-scope only
/// (every variant currently assigned to any slot in any binder, across all users - price data is
/// a shared fact about a card, not per-user data); an "all cards" or "watchlist" scope can be
/// added later as another implementation of this interface.
/// </summary>
public interface IScrapeScopeProvider
{
    Task<IReadOnlyList<Guid>> GetInScopeCardVariantIdsAsync(CancellationToken ct);
}
