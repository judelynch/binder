import { useState } from 'react'
import { Modal } from '../Modal'
import { useOverlayTags } from '../../lib/queries/overlay-tags'
import { useSetOverlayTag, useUnassignSlot, useUpdateSlotState } from '../../lib/queries/spread'
import type { BinderSlot } from '../../lib/spread-types'
import { OverlayTagPicker } from './OverlayTagPicker'

export function SlotActionPanel({
  binderId,
  spreadIndex,
  slot,
  onClose,
}: {
  binderId: string
  spreadIndex: number
  slot: BinderSlot
  onClose: () => void
}) {
  const [confirmingRemove, setConfirmingRemove] = useState(false)
  const { data: tags } = useOverlayTags()
  const updateState = useUpdateSlotState(binderId, spreadIndex)
  const unassign = useUnassignSlot(binderId, spreadIndex)
  const setTag = useSetOverlayTag(binderId, spreadIndex)

  if (!slot.card) return null

  return (
    <Modal title={slot.card.name} onClose={onClose}>
      <div className="space-y-5">
        <div className="flex items-center gap-3">
          <div className="h-20 w-14 shrink-0 overflow-hidden rounded-md bg-canvas">
            {slot.card.imageSmallUrl && (
              <img src={slot.card.imageSmallUrl} alt={slot.card.name} className="h-full w-full object-cover" />
            )}
          </div>
          <div className="min-w-0">
            <div className="truncate text-sm font-semibold text-ink">{slot.card.name}</div>
            <div className="text-xs text-ink-soft">
              {slot.card.setName} · #{slot.card.number}
              {slot.card.rarity ? ` · ${slot.card.rarity}` : ''}
            </div>
            {slot.variantTypeName && <div className="mt-0.5 text-xs text-ink-faint">{slot.variantTypeName}</div>}
          </div>
        </div>

        <label className="flex items-center justify-between rounded-lg border border-border px-3 py-2.5">
          <span className="text-sm font-medium text-ink">I own this card</span>
          <input
            type="checkbox"
            checked={slot.owned}
            onChange={(e) => updateState.mutate({ slotId: slot.slotId, owned: e.target.checked })}
            className="h-4 w-4 accent-[var(--color-accent)]"
          />
        </label>

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
