import type { PanelSide } from '../../lib/panel-nav'
import type { BinderSlot, SlotSuggestions, SpreadPanel } from '../../lib/spread-types'
import { Pocket } from './Pocket'

export function PagePanel({
  panel,
  side,
  columns,
  binderColourHex,
  greyscaleEnabled,
  overlaysEnabled,
  onOpenSlot,
  suggestionsBySlot,
  onOpenSuggestions,
  onToggleOwned,
  onQuickRemove,
  selectMode,
  selectedSlotIds,
  onToggleSelect,
  isSlotDimmed,
}: {
  panel: SpreadPanel
  side: PanelSide
  columns: number
  binderColourHex: string
  greyscaleEnabled: boolean
  overlaysEnabled: boolean
  onOpenSlot: (slot: BinderSlot) => void
  suggestionsBySlot?: Map<string, SlotSuggestions>
  onOpenSuggestions?: (slot: BinderSlot) => void
  onToggleOwned?: (slot: BinderSlot) => void
  onQuickRemove?: (slot: BinderSlot) => void
  selectMode?: boolean
  selectedSlotIds?: ReadonlySet<string>
  onToggleSelect?: (slot: BinderSlot) => void
  isSlotDimmed?: (slot: BinderSlot) => boolean
}) {
  if (panel.type === 'cover') {
    return (
      <div className="flex items-center justify-center rounded-[10px] bg-surface-2 p-5">
        <div className="text-center text-ink-faint">
          <div
            className="mx-auto mb-2.5 h-11 w-11 rounded-full border-[3px] opacity-60"
            style={{ borderColor: binderColourHex }}
          />
          <div className="text-[11px] font-semibold uppercase tracking-wide">
            {side === 'left' ? 'Front Cover' : 'Back Cover'}
          </div>
        </div>
      </div>
    )
  }

  return (
    <div className="relative rounded-[10px] bg-surface-2 p-4 pt-6 shadow-[inset_0_0_0_1px_rgba(255,255,255,0.02)] sm:p-5 sm:pt-6">
      <div
        className={`absolute top-6 rounded px-2 py-0.5 text-[10.5px] font-bold text-accent-ink ${
          side === 'left' ? '-left-1.5 rounded-l' : '-right-1.5 rounded-r'
        }`}
        style={{ background: binderColourHex }}
      >
        PAGE {panel.pageNumber}
      </div>
      <div className="grid gap-2" style={{ gridTemplateColumns: `repeat(${columns}, 1fr)` }}>
        {panel.slots?.map((slot) => (
          <Pocket
            key={slot.slotId}
            slot={slot}
            binderColourHex={binderColourHex}
            greyscaleEnabled={greyscaleEnabled}
            overlaysEnabled={overlaysEnabled}
            onOpen={() => onOpenSlot(slot)}
            hasSuggestions={(suggestionsBySlot?.get(slot.slotId)?.suggestions.length ?? 0) > 0}
            onOpenSuggestions={onOpenSuggestions ? () => onOpenSuggestions(slot) : undefined}
            onToggleOwned={onToggleOwned ? () => onToggleOwned(slot) : undefined}
            onQuickRemove={onQuickRemove ? () => onQuickRemove(slot) : undefined}
            selectMode={selectMode}
            selected={selectedSlotIds?.has(slot.slotId)}
            onToggleSelect={onToggleSelect ? () => onToggleSelect(slot) : undefined}
            dimmed={isSlotDimmed ? isSlotDimmed(slot) : false}
          />
        ))}
      </div>
    </div>
  )
}
