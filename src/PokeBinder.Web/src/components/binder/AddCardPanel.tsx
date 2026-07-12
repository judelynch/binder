import { useState } from 'react'
import { Modal } from '../Modal'
import { useSetCards, useSets } from '../../lib/queries/cards'
import { useAssignCard } from '../../lib/queries/spread'
import { useDebouncedValue } from '../../lib/useDebouncedValue'

export function AddCardPanel({
  binderId,
  spreadIndex,
  slotId,
  onClose,
}: {
  binderId: string
  spreadIndex: number
  slotId: string
  onClose: () => void
}) {
  const { data: sets } = useSets()
  const [setId, setSetId] = useState<string | null>(null)
  const [nameInput, setNameInput] = useState('')
  const debouncedName = useDebouncedValue(nameInput, 250)
  const { data: cardsPage, isFetching } = useSetCards(setId, debouncedName)
  const assignCard = useAssignCard(binderId, spreadIndex)

  return (
    <Modal title="Add a card" onClose={onClose}>
      <div className="space-y-4">
        <p className="text-xs text-ink-faint">
          Basic name search within a set — full cross-set search arrives in a later phase.
        </p>

        <div>
          <label htmlFor="add-card-set" className="mb-1.5 block text-xs font-semibold text-ink-soft">
            Set
          </label>
          <select
            id="add-card-set"
            value={setId ?? ''}
            onChange={(e) => setSetId(e.target.value || null)}
            className="w-full rounded-lg border border-border bg-canvas px-3 py-2 text-sm text-ink focus:border-accent focus:outline-none"
          >
            <option value="">Choose a set…</option>
            {sets?.map((set) => (
              <option key={set.id} value={set.id}>
                {set.name}
              </option>
            ))}
          </select>
        </div>

        {setId && (
          <div>
            <label htmlFor="add-card-name" className="mb-1.5 block text-xs font-semibold text-ink-soft">
              Card name
            </label>
            <input
              id="add-card-name"
              type="text"
              value={nameInput}
              onChange={(e) => setNameInput(e.target.value)}
              placeholder="e.g. Charizard"
              className="w-full rounded-lg border border-border bg-canvas px-3 py-2 text-sm text-ink placeholder:text-ink-faint focus:border-accent focus:outline-none"
            />
          </div>
        )}

        {setId && (
          <div className="max-h-72 space-y-1.5 overflow-y-auto">
            {isFetching ? (
              <p className="py-6 text-center text-xs text-ink-faint">Searching…</p>
            ) : cardsPage && cardsPage.items.length > 0 ? (
              cardsPage.items.map((card) => {
                const variantId = card.variants[0]?.id
                return (
                  <button
                    key={card.id}
                    type="button"
                    disabled={!variantId || assignCard.isPending}
                    onClick={() => variantId && assignCard.mutate({ slotId, cardVariantId: variantId }, { onSuccess: onClose })}
                    className="flex w-full items-center gap-3 rounded-lg border border-border px-2.5 py-2 text-left hover:border-accent disabled:opacity-40"
                  >
                    <div className="h-12 w-9 shrink-0 overflow-hidden rounded bg-surface-2">
                      {card.imageSmallUrl && (
                        <img src={card.imageSmallUrl} alt={card.name} loading="lazy" className="h-full w-full object-cover" />
                      )}
                    </div>
                    <div className="min-w-0">
                      <div className="truncate text-sm text-ink">{card.name}</div>
                      <div className="text-xs text-ink-soft">
                        #{card.number}
                        {card.rarity ? ` · ${card.rarity}` : ''}
                      </div>
                    </div>
                  </button>
                )
              })
            ) : (
              <p className="py-6 text-center text-xs text-ink-faint">No cards found.</p>
            )}
          </div>
        )}
      </div>
    </Modal>
  )
}
