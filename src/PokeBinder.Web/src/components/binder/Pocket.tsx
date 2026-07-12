import { useDraggable, useDroppable } from '@dnd-kit/core'
import { CSS } from '@dnd-kit/utilities'
import type { BinderSlot } from '../../lib/spread-types'
import { CardImage } from './CardImage'

export function Pocket({
  slot,
  greyscaleEnabled,
  overlaysEnabled,
  onOpen,
}: {
  slot: BinderSlot
  greyscaleEnabled: boolean
  overlaysEnabled: boolean
  onOpen: () => void
}) {
  const { attributes, listeners, setNodeRef: setDragRef, transform, isDragging } = useDraggable({
    id: slot.slotId,
    disabled: !slot.card,
  })
  const { setNodeRef: setDropRef, isOver } = useDroppable({ id: slot.slotId })

  const setRefs = (node: HTMLElement | null) => {
    setDragRef(node)
    setDropRef(node)
  }

  const isEmpty = !slot.card
  const isGreyscale = greyscaleEnabled && slot.card && !slot.owned

  return (
    <button
      ref={setRefs}
      type="button"
      onClick={onOpen}
      {...listeners}
      {...attributes}
      style={{ transform: CSS.Translate.toString(transform), opacity: isDragging ? 0.4 : 1 }}
      aria-label={isEmpty ? 'Empty slot — add a card' : `${slot.card!.name}, ${slot.owned ? 'owned' : 'not owned'}`}
      className={`relative aspect-[5/7] overflow-hidden rounded-lg text-left transition-shadow ${
        isEmpty
          ? 'flex items-center justify-center border-2 border-dashed border-border bg-transparent hover:border-accent'
          : 'bg-canvas shadow-[inset_0_2px_5px_rgba(0,0,0,0.65),inset_0_-1px_0_rgba(255,255,255,0.04)]'
      } ${isOver ? 'ring-2 ring-accent' : ''}`}
    >
      {isEmpty ? (
        <span className="text-2xl font-light text-ink-faint">+</span>
      ) : (
        <>
          <CardImage src={slot.card!.imageSmallUrl} alt={slot.card!.name} greyscale={!!isGreyscale} />
          <div className="pointer-events-none absolute inset-0 bg-gradient-to-r from-white/5 to-transparent" />
          {overlaysEnabled && slot.overlayTag && (
            <>
              <div
                className="pointer-events-none absolute inset-0"
                style={{ background: `${slot.overlayTag.colourHex}4d` }}
              />
              <span
                className="absolute bottom-1 left-1 rounded-full px-1.5 py-0.5 text-[8px] font-bold text-accent-ink"
                style={{ background: slot.overlayTag.colourHex }}
              >
                {slot.overlayTag.name}
              </span>
            </>
          )}
          <span
            className="absolute right-1 top-1 h-1.5 w-1.5 rounded-full ring-2 ring-black/40"
            style={{ background: slot.owned ? 'var(--color-good)' : 'var(--color-bad)' }}
          />
        </>
      )}
    </button>
  )
}
