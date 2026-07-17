import { CardImage } from '../binder/CardImage'
import type { VariantSummary } from '../../lib/queries/cards'
import type { CardSearchResult } from '../../lib/search-types'

export function CardResultTile({
  card,
  variant,
  selected,
  onToggleSelect,
}: {
  card: CardSearchResult
  variant: VariantSummary
  selected: boolean
  onToggleSelect: () => void
}) {
  return (
    <button
      type="button"
      onClick={onToggleSelect}
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
        <span className="absolute bottom-1 left-1 right-1 truncate rounded bg-black/60 px-1.5 py-0.5 text-center text-[9px] font-bold uppercase tracking-wide text-white">
          {variant.variantTypeName}
        </span>
      </div>
      <div className="mt-1.5 truncate text-xs font-semibold text-ink">{card.name}</div>
      <div className="truncate text-[10.5px] text-ink-soft">
        {card.setName} · #{card.number}
      </div>
      <div className="truncate text-[10.5px] text-ink-faint">{card.rarity ?? ' '}</div>
    </button>
  )
}
