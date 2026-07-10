namespace PokeBinder.Infrastructure.Cards.Import;

public interface ICardDataSource
{
    Task<string> ReadSetsJsonAsync(CancellationToken ct);
    Task<IReadOnlyList<string>> GetCardSetIdsAsync(CancellationToken ct);
    Task<string> ReadCardsJsonAsync(string setId, CancellationToken ct);
}
