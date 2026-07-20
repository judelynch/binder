import { CardImage } from '../binder/CardImage'
import type { CardSummary, OwnedVariantSummary } from '../../lib/queries/cards'

/**
 * One tile per (card, variant) pair, mirroring search/CardResultTile.tsx's visual language and
 * click-to-select interaction. The top-right circle is the transient "selected for the pending
 * bulk mark-owned/unowned action" state (same as CardResultTile's "selected for insert"); the
 * bottom-right badge is the persistent "you already own this" readout - the two are independent
 * booleans, not the same thing, and both need to be visible at once. Double-click opens the full
 * card detail page (price/chart/history) - single click stays a plain selection toggle so the two
 * interactions don't fight each other.
 */
export function OwnershipVariantTile({
  card,
  variant,
  selected,
  onToggleSelect,
  onOpenDetail,
  priceGbp,
}: {
  card: CardSummary
  variant: OwnedVariantSummary
  selected: boolean
  onToggleSelect: () => void
  onOpenDetail?: () => void
  priceGbp?: number | null
}) {
  return (
    <button
      type="button"
      onClick={onToggleSelect}
      onDoubleClick={onOpenDetail}
      aria-pressed={selected}
      aria-label={`${selected ? 'Deselect' : 'Select'} ${card.name} (${variant.variantTypeName})`}
      className={`relative block w-full rounded-lg border p-1.5 text-left transition-colors ${
        selected ? 'border-accent bg-accent/10' : 'border-border bg-surface'
      }`}
    >
      <div className="relative aspect-[5/7] overflow-hidden rounded-md bg-canvas">
        <CardImage src={card.imageSmallUrl} alt={card.name} greyscale={false} />
        <span
          className={`absolute right-1.5 top-1.5 flex h-5 w-5 items-center justify-center rounded-full border-2 text-[10px] font-bold ${
            selected ? 'border-accent bg-accent text-accent-ink' : 'border-white/70 bg-black/30 text-transparent'
          }`}
        >
          ✓
        </span>
        {variant.owned && (
          <span
            aria-label="You own this"
            className="absolute bottom-1 right-1 flex h-5 w-5 items-center justify-center rounded-full bg-accent text-[11px] font-bold text-accent-ink shadow"
          >
            ✓
          </span>
        )}
        <span className="absolute bottom-1 left-1 truncate rounded bg-black/60 px-1.5 py-0.5 text-[9px] font-bold uppercase tracking-wide text-white">
          {variant.variantTypeName}
        </span>
        {priceGbp != null && (
          <span className="absolute left-1 top-1.5 rounded bg-black/65 px-1 py-0.5 text-[9px] font-bold text-white [font-variant-numeric:tabular-nums]">
            £{priceGbp.toFixed(2)}
          </span>
        )}
      </div>
      <div className="mt-1.5 truncate text-xs font-semibold text-ink">{card.name}</div>
      <div className="truncate text-[10.5px] text-ink-soft">#{card.number}</div>
    </button>
  )
}
