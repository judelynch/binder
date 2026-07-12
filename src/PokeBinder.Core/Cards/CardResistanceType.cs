namespace PokeBinder.Core.Cards;

/// <summary>Indexed join table backing Card.Resistances' type field for filtering — Resistances itself stays the JSON source of truth.</summary>
public class CardResistanceType
{
    public string CardId { get; set; } = string.Empty;
    public Card? Card { get; set; }
    public string Type { get; set; } = string.Empty;
}
