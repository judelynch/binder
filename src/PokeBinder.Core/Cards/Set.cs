namespace PokeBinder.Core.Cards;

public class Set
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Series { get; set; } = string.Empty;
    public int PrintedTotal { get; set; }
    public int Total { get; set; }
    public DateOnly ReleaseDate { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? PtcgoCode { get; set; }
    public string? SymbolImageUrl { get; set; }
    public string? LogoImageUrl { get; set; }
    public IReadOnlyDictionary<string, string> Legalities { get; set; } = new Dictionary<string, string>();

    public ICollection<Card> Cards { get; set; } = new List<Card>();
}
