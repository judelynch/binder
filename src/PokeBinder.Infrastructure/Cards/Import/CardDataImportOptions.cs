namespace PokeBinder.Infrastructure.Cards.Import;

public class CardDataImportOptions
{
    public const string SectionName = "CardData";

    /// <summary>If set, the importer reads from this local checkout of pokemon-tcg-data instead of downloading.</summary>
    public string? LocalPath { get; set; }

    public string TarballUrl { get; set; } =
        "https://codeload.github.com/PokemonTCG/pokemon-tcg-data/tar.gz/refs/heads/master";
}
