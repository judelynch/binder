using PokeBinder.Core.Binders;
using PokeBinder.Core.Cards;

namespace PokeBinder.Core.Collection;

/// <summary>
/// A user's ownership of a specific card variant, independent of whether (or where)
/// that variant is placed in any binder. BinderSlot.Owned tracks slot occupancy;
/// this tracks the underlying collection fact and is never synced with it.
/// </summary>
public class CardOwnership
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public Guid CardVariantId { get; set; }
    public CardVariant? CardVariant { get; set; }

    /// <summary>Always &gt;= 1. Un-owning a variant deletes the row rather than zeroing it.</summary>
    public int Quantity { get; set; }

    public CardCondition? Condition { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
