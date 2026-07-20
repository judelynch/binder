import { useDraggable, useDroppable } from '@dnd-kit/core'
import { CSS } from '@dnd-kit/utilities'
import type { BinderSlot } from '../../lib/spread-types'
import { CardImage } from './CardImage'
import { LightbulbIcon } from './LightbulbIcon'

export function Pocket({
  slot,
  binderColourHex,
  greyscaleEnabled,
  overlaysEnabled,
  onOpen,
  hasSuggestions,
  onOpenSuggestions,
  onToggleOwned,
  onQuickRemove,
  selectMode,
  selected,
  onToggleSelect,
  dimmed,
  costToBuyGbp,
}: {
  slot: BinderSlot
  binderColourHex: string
  greyscaleEnabled: boolean
  overlaysEnabled: boolean
  onOpen: () => void
  hasSuggestions?: boolean
  onOpenSuggestions?: () => void
  onToggleOwned?: () => void
  onQuickRemove?: () => void
  selectMode?: boolean
  selected?: boolean
  onToggleSelect?: () => void
  dimmed?: boolean
  costToBuyGbp?: number | null
}) {
  // Draggable/droppable regardless of select mode - a plain click (no movement past dnd-kit's
  // activation distance) still reaches onClick below, so drag and click-to-toggle-select coexist
  // the same way drag and click-to-open already do outside select mode. This is what lets dragging
  // a selected card move the whole multi-selection together (see BinderFrame's onDragEnd).
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
  const canSelect = selectMode && !isEmpty && onToggleSelect

  return (
    <div className={`relative transition-opacity ${dimmed ? 'opacity-20' : ''}`}>
      <button
        ref={setRefs}
        type="button"
        onClick={canSelect ? onToggleSelect : onOpen}
        {...listeners}
        {...attributes}
        style={{
          transform: CSS.Translate.toString(transform),
          opacity: isDragging ? 0.4 : 1,
          borderColor: isEmpty ? `${binderColourHex}80` : undefined,
          boxShadow: isEmpty
            ? undefined
            : `inset 0 2px 5px rgba(0,0,0,0.65), inset 0 -1px 0 rgba(255,255,255,0.04), inset 0 0 0 1.5px ${binderColourHex}40`,
        }}
        aria-label={isEmpty ? 'Empty slot — add a card' : `${slot.card!.name}, ${slot.owned ? 'owned' : 'not owned'}`}
        aria-pressed={canSelect ? !!selected : undefined}
        className={`relative aspect-[5/7] w-full overflow-hidden rounded-lg text-left transition-shadow ${
          isEmpty ? 'flex items-center justify-center border-2 border-dashed bg-transparent hover:border-accent' : 'bg-canvas'
        } ${isOver ? 'ring-2 ring-accent' : ''} ${selected ? 'ring-2 ring-accent' : ''}`}
      >
        {isEmpty ? (
          <span className="text-2xl font-light text-ink-faint">+</span>
        ) : (
          <>
            <CardImage src={slot.card!.imageSmallUrl} alt={slot.card!.name} greyscale={!!isGreyscale} />
            <div className="pointer-events-none absolute inset-0 bg-gradient-to-r from-white/5 to-transparent" />
            {slot.variantTypeName && slot.variantTypeName !== 'Normal' && (
              <span className="pointer-events-none absolute inset-x-0 top-0 truncate bg-black/60 px-1 py-0.5 text-center text-[8px] font-bold uppercase tracking-wide text-white">
                {slot.variantTypeName}
              </span>
            )}
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
            {!selectMode && onToggleOwned ? (
              <button
                type="button"
                onClick={(e) => {
                  e.stopPropagation()
                  onToggleOwned()
                }}
                aria-label={slot.owned ? 'Mark as not owned' : 'Mark as owned'}
                title={slot.owned ? 'Owned — click to unmark' : 'Not owned — click to mark owned'}
                className="absolute right-0 top-0 flex h-6 w-6 items-center justify-center"
              >
                <span
                  className="h-1.5 w-1.5 rounded-full ring-2 ring-black/40"
                  style={{ background: slot.owned ? 'var(--color-good)' : 'var(--color-bad)' }}
                />
              </button>
            ) : (
              <span
                className="absolute right-1 top-1 h-1.5 w-1.5 rounded-full ring-2 ring-black/40"
                style={{ background: slot.owned ? 'var(--color-good)' : 'var(--color-bad)' }}
              />
            )}
            {selected && (
              <span className="absolute inset-0 flex items-center justify-center bg-accent/25">
                <span className="flex h-6 w-6 items-center justify-center rounded-full bg-accent text-xs font-bold text-accent-ink">
                  ✓
                </span>
              </span>
            )}
          </>
        )}
      </button>

      {!isEmpty && !selectMode && onQuickRemove && (
        <button
          type="button"
          onClick={(e) => {
            e.stopPropagation()
            onQuickRemove()
          }}
          aria-label="Remove card from this slot"
          title="Remove card"
          className="absolute left-1 top-1 flex h-5 w-5 items-center justify-center rounded-full bg-black/50 text-[11px] font-bold leading-none text-white shadow ring-1 ring-black/40 hover:bg-bad"
        >
          ×
        </button>
      )}

      {isEmpty && !selectMode && hasSuggestions && onOpenSuggestions && (
        <button
          type="button"
          onClick={(e) => {
            e.stopPropagation()
            onOpenSuggestions()
          }}
          aria-label="Suggested card available"
          title="Suggested card available"
          className="absolute bottom-1 right-1 flex h-5 w-5 items-center justify-center rounded-full bg-accent text-accent-ink shadow ring-2 ring-black/40 hover:brightness-110"
        >
          <LightbulbIcon className="h-3 w-3" />
        </button>
      )}

      {!isEmpty && !slot.owned && !selectMode && costToBuyGbp != null && (
        <span
          title="Cheapest available price to buy this card"
          className="pointer-events-none absolute bottom-1 left-1 rounded bg-black/65 px-1 py-0.5 text-[9px] font-bold text-white shadow ring-1 ring-black/40 [font-variant-numeric:tabular-nums]"
        >
          £{costToBuyGbp.toFixed(2)}
        </span>
      )}
    </div>
  )
}
