import { Modal } from '../Modal'
import { CardImage } from './CardImage'
import { useAddSuggestedCard } from '../../lib/queries/spread'
import type { BinderSlot, SuggestedCard, SuggestionReason } from '../../lib/spread-types'

const REASON_LABELS: Record<SuggestionReason, string> = {
  NextInSet: 'Next card in this set',
  PrevInSet: 'Previous card in this set',
  NextRelease: `Next release of this card's Pokémon`,
  SameThemeRarity: 'Matches this binder’s theme',
}

export function SuggestionModal({
  slot,
  suggestions,
  basedOnCardName,
  binderId,
  spreadIndex,
  onClose,
}: {
  slot: BinderSlot
  suggestions: SuggestedCard[]
  basedOnCardName: string
  binderId: string
  spreadIndex: number
  onClose: () => void
}) {
  const addCard = useAddSuggestedCard(binderId, spreadIndex)

  return (
    <Modal title="Suggested cards" onClose={onClose}>
      <p className="mb-3 text-xs text-ink-soft">
        Based on <span className="font-semibold text-ink">{basedOnCardName}</span> nearby.
      </p>
      <div className="space-y-3">
        {suggestions.map((card) => (
          <div key={`${card.cardId}-${card.reason}`} className="flex items-center gap-3 rounded-lg border border-border p-2.5">
            <div className="relative h-16 w-11 shrink-0 overflow-hidden rounded bg-canvas">
              <CardImage src={card.imageSmallUrl} alt={card.name} greyscale={false} />
            </div>
            <div className="min-w-0 flex-1">
              <div className="truncate text-sm font-semibold text-ink">{card.name}</div>
              <div className="truncate text-xs text-ink-soft">
                {card.setName} · #{card.number}
                {card.rarity ? ` · ${card.rarity}` : ''}
              </div>
              <div className="mt-0.5 text-[11px] text-ink-faint">{REASON_LABELS[card.reason]}</div>
            </div>
            <button
              type="button"
              disabled={addCard.isPending}
              onClick={() => addCard.mutate({ fromSlotId: slot.slotId, cardVariantId: card.cardVariantId }, { onSuccess: onClose })}
              className="shrink-0 rounded-lg bg-accent px-3 py-1.5 text-xs font-semibold text-accent-ink disabled:opacity-50"
            >
              {addCard.isPending ? 'Adding…' : 'Add'}
            </button>
          </div>
        ))}
      </div>
      {addCard.isError && <p className="mt-3 text-xs text-bad">Could not add that card. Try again.</p>}
    </Modal>
  )
}
