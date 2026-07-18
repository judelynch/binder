import { useState } from 'react'
import { Modal } from '../Modal'
import { useOverlayTags } from '../../lib/queries/overlay-tags'
import { useSetOverlayTag, useUnassignSlot, useUpdateSlotState } from '../../lib/queries/spread'
import { isStale, pickBucket, type CardVariantPrice } from '../../lib/pricing-types'
import type { BinderSlot, SlotCondition } from '../../lib/spread-types'
import { OverlayTagPicker } from './OverlayTagPicker'

const CONDITIONS: SlotCondition[] = ['NM', 'LP', 'MP', 'HP', 'DMG']

function PriceSection({ price, slotCondition }: { price: CardVariantPrice; slotCondition: string | null }) {
  const [showDelivered, setShowDelivered] = useState(false)

  const targetCondition = slotCondition ?? 'Unspecified'
  const rawBucket =
    pickBucket(price.rawBuckets, (b) => b.condition === targetCondition) ?? pickBucket(price.rawBuckets, () => true)
  const psa10 = pickBucket(price.gradedBuckets, (b) => b.grader === 'PSA' && b.grade === 10)
  const psa9 = pickBucket(price.gradedBuckets, (b) => b.grader === 'PSA' && b.grade === 9)

  if (!rawBucket && !psa10 && !psa9) return null

  const amount = (b: { itemOnlyMedianGbp: number; deliveredMedianGbp: number }) =>
    showDelivered ? b.deliveredMedianGbp : b.itemOnlyMedianGbp

  return (
    <div className="rounded-lg border border-border bg-surface-2 px-4 py-3">
      <div className="flex items-center justify-between">
        <span className="text-xs font-semibold uppercase tracking-wide text-ink-faint">Market price</span>
        <button
          type="button"
          onClick={() => setShowDelivered((v) => !v)}
          className="text-[10.5px] font-semibold text-accent"
        >
          {showDelivered ? 'Showing delivered' : 'Showing item only'}
        </button>
      </div>

      {rawBucket && (
        <div className="mt-2 flex items-baseline justify-between">
          <span className="text-sm text-ink-soft">Raw · {rawBucket.condition ?? 'Unspecified'}</span>
          <span className="text-lg font-semibold text-ink [font-variant-numeric:tabular-nums]">
            £{amount(rawBucket).toFixed(2)}
          </span>
        </div>
      )}

      {(psa10 || psa9) && (
        <div className="mt-2 flex items-center gap-4 border-t border-border pt-2">
          {psa10 && (
            <div>
              <div className="text-xs text-ink-faint">PSA 10</div>
              <div className="text-sm font-semibold text-ink [font-variant-numeric:tabular-nums]">£{amount(psa10).toFixed(2)}</div>
            </div>
          )}
          {psa9 && (
            <div>
              <div className="text-xs text-ink-faint">PSA 9</div>
              <div className="text-sm font-semibold text-ink [font-variant-numeric:tabular-nums]">£{amount(psa9).toFixed(2)}</div>
            </div>
          )}
        </div>
      )}

      {rawBucket && (
        <div className="mt-2 text-[10.5px] text-ink-faint">
          {rawBucket.sampleCount} sale{rawBucket.sampleCount === 1 ? '' : 's'} in the last {rawBucket.windowDays} days · as of{' '}
          {new Date(rawBucket.lastSaleDate).toLocaleDateString()}
        </div>
      )}
      {isStale(price.lastScrapedAt) && (
        <div className="mt-1 text-[10.5px] text-accent">
          {price.lastScrapedAt ? `Prices last checked ${new Date(price.lastScrapedAt).toLocaleDateString()} — may be out of date.` : 'Prices not yet checked for this card.'}
        </div>
      )}
    </div>
  )
}

export function SlotActionPanel({
  binderId,
  spreadIndex,
  slot,
  price,
  onClose,
}: {
  binderId: string
  spreadIndex: number
  slot: BinderSlot
  price?: CardVariantPrice | null
  onClose: () => void
}) {
  const [confirmingRemove, setConfirmingRemove] = useState(false)
  const { data: tags } = useOverlayTags()
  const updateState = useUpdateSlotState(binderId, spreadIndex)
  const unassign = useUnassignSlot(binderId, spreadIndex)
  const setTag = useSetOverlayTag(binderId, spreadIndex)

  if (!slot.card) return null

  const quantity = slot.quantity ?? 1

  function setQuantity(next: number) {
    if (next < 1) return
    updateState.mutate({ slotId: slot.slotId, quantity: next })
  }

  return (
    <Modal title={slot.card.name} onClose={onClose} size="lg">
      <div className="space-y-6">
        <div className="flex items-center gap-4">
          <div className="h-28 w-20 shrink-0 overflow-hidden rounded-lg bg-canvas">
            {slot.card.imageSmallUrl && (
              <img src={slot.card.imageSmallUrl} alt={slot.card.name} className="h-full w-full object-cover" />
            )}
          </div>
          <div className="min-w-0">
            <div className="truncate font-display text-lg font-semibold text-ink">{slot.card.name}</div>
            <div className="text-sm text-ink-soft">
              {slot.card.setName} · #{slot.card.number}
              {slot.card.rarity ? ` · ${slot.card.rarity}` : ''}
            </div>
            {slot.variantTypeName && <div className="mt-0.5 text-sm text-ink-faint">{slot.variantTypeName}</div>}
          </div>
        </div>

        {price && <PriceSection price={price} slotCondition={slot.condition} />}

        <div>
          <button
            type="button"
            onClick={() => updateState.mutate({ slotId: slot.slotId, owned: !slot.owned })}
            aria-pressed={slot.owned}
            className={`flex w-full items-center justify-between rounded-lg border px-4 py-3 text-left transition-colors ${
              slot.owned ? 'border-accent bg-accent/10' : 'border-border bg-surface-2'
            }`}
          >
            <span className="text-sm font-semibold text-ink">{slot.owned ? 'You own this card' : "I don't own this card yet"}</span>
            <span
              className={`flex h-7 w-7 items-center justify-center rounded-full border-2 text-sm font-bold ${
                slot.owned ? 'border-accent bg-accent text-accent-ink' : 'border-ink-faint text-transparent'
              }`}
            >
              ✓
            </span>
          </button>

          {slot.owned && (
            <div className="mt-3 flex items-center gap-4 rounded-lg border border-border px-4 py-3">
              <div className="flex items-center gap-2">
                <span className="text-xs font-semibold text-ink-soft">Quantity</span>
                <button
                  type="button"
                  onClick={() => setQuantity(quantity - 1)}
                  disabled={quantity <= 1}
                  className="flex h-7 w-7 items-center justify-center rounded border border-border text-ink-soft disabled:opacity-30"
                >
                  −
                </button>
                <span className="w-5 text-center text-sm text-ink [font-variant-numeric:tabular-nums]">{quantity}</span>
                <button
                  type="button"
                  onClick={() => setQuantity(quantity + 1)}
                  className="flex h-7 w-7 items-center justify-center rounded border border-border text-ink-soft"
                >
                  +
                </button>
              </div>

              <div className="h-6 w-px bg-border" />

              <div className="flex items-center gap-2">
                <span className="text-xs font-semibold text-ink-soft">Condition</span>
                <select
                  value={slot.condition ?? ''}
                  onChange={(e) => updateState.mutate({ slotId: slot.slotId, condition: e.target.value as SlotCondition })}
                  className="rounded border border-border bg-canvas px-2 py-1 text-sm text-ink"
                >
                  <option value="" disabled>
                    Set condition
                  </option>
                  {CONDITIONS.map((c) => (
                    <option key={c} value={c}>
                      {c}
                    </option>
                  ))}
                </select>
              </div>
            </div>
          )}
        </div>

        <div>
          <div className="mb-1.5 text-xs font-semibold text-ink-soft">Overlay tag</div>
          <OverlayTagPicker
            tags={tags ?? []}
            selectedId={slot.overlayTag?.id ?? null}
            onSelect={(id) => setTag.mutate({ slotId: slot.slotId, overlayTagId: id })}
          />
        </div>

        {confirmingRemove ? (
          <div className="rounded-lg border border-bad/40 bg-bad/10 p-3">
            <p className="text-xs text-ink">
              {slot.owned ? 'You marked this card as owned. ' : ''}Remove it from this slot?
            </p>
            <div className="mt-2.5 flex justify-end gap-2">
              <button
                type="button"
                onClick={() => setConfirmingRemove(false)}
                className="rounded-lg border border-border px-3 py-1.5 text-xs font-semibold text-ink-soft"
              >
                Cancel
              </button>
              <button
                type="button"
                onClick={() => unassign.mutate(slot.slotId, { onSuccess: onClose })}
                className="rounded-lg bg-bad px-3 py-1.5 text-xs font-semibold text-white"
              >
                Remove
              </button>
            </div>
          </div>
        ) : (
          <button
            type="button"
            onClick={() => (slot.owned ? setConfirmingRemove(true) : unassign.mutate(slot.slotId, { onSuccess: onClose }))}
            className="w-full rounded-lg border border-border py-2 text-sm font-semibold text-bad"
          >
            Remove card
          </button>
        )}
      </div>
    </Modal>
  )
}
