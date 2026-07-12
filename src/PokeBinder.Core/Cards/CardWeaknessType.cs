namespace PokeBinder.Core.Cards;

/// <summary>Indexed join table backing Card.Weaknesses' type field for filtering — Weaknesses itself stays the JSON source of truth.</summary>
public class CardWeaknessType
{
    public string CardId { get; set; } = string.Empty;
    public Card? Card { get; set; }
    public string Type { get; set; } = string.Empty;
}
