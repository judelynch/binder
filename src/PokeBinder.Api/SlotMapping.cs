using PokeBinder.Api.Dtos;
using PokeBinder.Core.Binders;

namespace PokeBinder.Api;

public static class SlotMapping
{
    /// <summary>Requires CardVariant.Card.Set, CardVariant.VariantType, and OverlayTag to already be loaded.</summary>
    public static BinderSlotDto ToDto(BinderSlot slot)
    {
        CardSlotSummaryDto? card = null;
        string? variantTypeName = null;

        if (slot.CardVariant is not null)
        {
            var c = slot.CardVariant.Card!;
            card = new CardSlotSummaryDto(c.Id, c.Name, c.ImageSmallUrl, c.ImageLargeUrl, c.SetId, c.Set!.Name, c.Number, c.Rarity);
            variantTypeName = slot.CardVariant.VariantType!.Name;
        }

        OverlayTagDto? overlayTag = slot.OverlayTag is null
            ? null
            : new OverlayTagDto(slot.OverlayTag.Id, slot.OverlayTag.Name, slot.OverlayTag.ColourHex);

        return new BinderSlotDto(
            slot.Id, slot.Position, card, variantTypeName,
            slot.Owned, slot.Quantity, slot.Condition?.ToString(), overlayTag);
    }
}
