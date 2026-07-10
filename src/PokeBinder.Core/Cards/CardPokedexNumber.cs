namespace PokeBinder.Core.Cards;

public class CardPokedexNumber
{
    public string CardId { get; set; } = string.Empty;
    public Card? Card { get; set; }
    public int Number { get; set; }
}
