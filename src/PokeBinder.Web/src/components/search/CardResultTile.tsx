import { CardImage } from '../binder/CardImage'
import type { CardSearchResult } from '../../lib/search-types'

export function CardResultTile({
  card,
  selected,
  onToggleSelect,
  selectedVariantId,
  onSelectVariant,
}: {
  card: CardSearchResult
  selected: boolean
  onToggleSelect: () => void
  selectedVariantId: string | undefined
  onSelectVariant: (variantId: string) => void
}) {
  return (
    <div
      className={`relative rounded-lg border p-1.5 transition-colors ${selected ? 'border-accent bg-accent/10' : 'border-border bg-surface'}`}
    >
      <button
        type="button"
        onClick={onToggleSelect}
        aria-pressed={selected}
        aria-label={`${selected ? 'Deselect' : 'Select'} ${card.name}`}
        className="block w-full"
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
        </div>
        <div className="mt-1.5 truncate text-left text-xs font-semibold text-ink">{card.name}</div>
        <div className="truncate text-left text-[10.5px] text-ink-soft">
          {card.setName} · #{card.number}
        </div>
        {card.rarity && <div className="truncate text-left text-[10.5px] text-ink-faint">{card.rarity}</div>}
      </button>

      {card.variants.length > 1 && (
        <select
          aria-label={`Variant for ${card.name}`}
          value={selectedVariantId ?? card.variants[0].id}
          onChange={(e) => onSelectVariant(e.target.value)}
          onClick={(e) => e.stopPropagation()}
          className="mt-1 w-full rounded border border-border bg-canvas px-1 py-0.5 text-[10px] text-ink"
        >
          {card.variants.map((v) => (
            <option key={v.id} value={v.id}>
              {v.variantTypeName}
            </option>
          ))}
        </select>
      )}
    </div>
  )
}
