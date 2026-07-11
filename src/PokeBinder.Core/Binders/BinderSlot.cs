using PokeBinder.Core.Cards;

namespace PokeBinder.Core.Binders;

public class BinderSlot
{
    public Guid Id { get; set; }
    public Guid PageId { get; set; }
    public BinderPage? Page { get; set; }

    /// <summary>0-based, left-to-right top-to-bottom within the page.</summary>
    public int Position { get; set; }

    public Guid? CardVariantId { get; set; }
    public CardVariant? CardVariant { get; set; }

    public bool Owned { get; set; }
    public int? Quantity { get; set; }
    public CardCondition? Condition { get; set; }

    public Guid? OverlayTagId { get; set; }
    public OverlayTag? OverlayTag { get; set; }
}
