namespace PokeBinder.Core.Cards;

public class CardVariant
{
    public Guid Id { get; set; }
    public string CardId { get; set; } = string.Empty;
    public Card? Card { get; set; }
    public Guid VariantTypeId { get; set; }
    public VariantType? VariantType { get; set; }
}
