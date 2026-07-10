using System.Formats.Tar;
using System.IO.Compression;

namespace PokeBinder.Infrastructure.Cards.Import;

/// <summary>Downloads the PokemonTCG/pokemon-tcg-data repo as a tarball, extracts it to a temp directory once, then reads from it like a local checkout.</summary>
public class GitHubTarballCardDataSource : ICardDataSource
{
    private readonly HttpClient _httpClient;
    private readonly string _tarballUrl;
    private LocalDirectoryCardDataSource? _inner;

    public GitHubTarballCardDataSource(HttpClient httpClient, string tarballUrl)
    {
        _httpClient = httpClient;
        _tarballUrl = tarballUrl;
    }

    private async Task<LocalDirectoryCardDataSource> GetInnerAsync(CancellationToken ct)
    {
        if (_inner is not null)
        {
            return _inner;
        }

        var extractDir = Path.Combine(Path.GetTempPath(), "pokebinder-card-data", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(extractDir);

        await using (var httpStream = await _httpClient.GetStreamAsync(_tarballUrl, ct))
        await using (var gzipStream = new GZipStream(httpStream, CompressionMode.Decompress))
        {
            await TarFile.ExtractToDirectoryAsync(gzipStream, extractDir, overwriteFiles: true, ct);
        }

        var repoRoot = Directory.EnumerateDirectories(extractDir).FirstOrDefault()
            ?? throw new InvalidOperationException("Downloaded tarball did not contain the expected repo root directory.");

        _inner = new LocalDirectoryCardDataSource(repoRoot);
        return _inner;
    }

    public async Task<string> ReadSetsJsonAsync(CancellationToken ct) =>
        await (await GetInnerAsync(ct)).ReadSetsJsonAsync(ct);

    public async Task<IReadOnlyList<string>> GetCardSetIdsAsync(CancellationToken ct) =>
        await (await GetInnerAsync(ct)).GetCardSetIdsAsync(ct);

    public async Task<string> ReadCardsJsonAsync(string setId, CancellationToken ct) =>
        await (await GetInnerAsync(ct)).ReadCardsJsonAsync(setId, ct);
}
