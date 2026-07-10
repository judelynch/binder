namespace PokeBinder.Infrastructure.Cards.Import;

/// <summary>Reads sets/en.json and cards/en/*.json from a local checkout of PokemonTCG/pokemon-tcg-data.</summary>
public class LocalDirectoryCardDataSource : ICardDataSource
{
    private readonly string _rootPath;

    public LocalDirectoryCardDataSource(string rootPath)
    {
        _rootPath = rootPath;
    }

    public Task<string> ReadSetsJsonAsync(CancellationToken ct) =>
        File.ReadAllTextAsync(Path.Combine(_rootPath, "sets", "en.json"), ct);

    public Task<IReadOnlyList<string>> GetCardSetIdsAsync(CancellationToken ct)
    {
        var dir = Path.Combine(_rootPath, "cards", "en");
        IReadOnlyList<string> ids = Directory.EnumerateFiles(dir, "*.json")
            .Select(f => Path.GetFileNameWithoutExtension(f))
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToList();
        return Task.FromResult(ids);
    }

    public Task<string> ReadCardsJsonAsync(string setId, CancellationToken ct) =>
        File.ReadAllTextAsync(Path.Combine(_rootPath, "cards", "en", $"{setId}.json"), ct);
}
