namespace PokeBinder.Core.Cards;

/// <summary>One manual admin edit to a card's mutable fields (rarity correction, image override, etc).</summary>
public class CardEditAudit
{
    public Guid Id { get; set; }
    public string CardId { get; set; } = string.Empty;
    public Card? Card { get; set; }
    public string EditedByUserId { get; set; } = string.Empty;
    public string EditedByEmail { get; set; } = string.Empty;
    public DateTime EditedAt { get; set; }
    public string Note { get; set; } = string.Empty;
    public IReadOnlyList<string> ChangedFields { get; set; } = Array.Empty<string>();
}
