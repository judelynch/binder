namespace PokeBinder.Core.Cards;

/// <summary>Indexed join table backing Card.Subtypes for filtering — Subtypes itself stays the JSON source of truth.</summary>
public class CardSubtype
{
    public string CardId { get; set; } = string.Empty;
    public Card? Card { get; set; }
    public string Subtype { get; set; } = string.Empty;
}
