namespace PokeBinder.Core.Cards;

/// <summary>Indexed join table backing Card.Types for filtering — Types itself stays the JSON source of truth.</summary>
public class CardType
{
    public string CardId { get; set; } = string.Empty;
    public Card? Card { get; set; }
    public string Type { get; set; } = string.Empty;
}
