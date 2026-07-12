import { DndContext, type DragEndEvent } from '@dnd-kit/core'
import { resolveDragMove } from '../../lib/dnd'
import type { PanelSide } from '../../lib/panel-nav'
import type { BinderSlot, Spread } from '../../lib/spread-types'
import { PagePanel } from './PagePanel'
import { Spine } from './Spine'

export function BinderFrame({
  spread,
  mode,
  singleSide,
  binderColourHex,
  columns,
  greyscaleEnabled,
  overlaysEnabled,
  onOpenSlot,
  onMoveSlot,
}: {
  spread: Spread
  mode: 'spread' | 'single'
  singleSide?: PanelSide
  binderColourHex: string
  columns: number
  greyscaleEnabled: boolean
  overlaysEnabled: boolean
  onOpenSlot: (slot: BinderSlot) => void
  onMoveSlot: (sourceSlotId: string, targetSlotId: string) => void
}) {
  function handleDragEnd(event: DragEndEvent) {
    const move = resolveDragMove(event)
    if (move) onMoveSlot(move.sourceSlotId, move.targetSlotId)
  }

  const panelProps = (side: PanelSide) => ({
    panel: side === 'left' ? spread.leftPanel : spread.rightPanel,
    side,
    columns,
    binderColourHex,
    greyscaleEnabled,
    overlaysEnabled,
    onOpenSlot,
  })

  return (
    <DndContext onDragEnd={handleDragEnd}>
      <div
        className="rounded-b-[18px] border border-t-0 border-border p-4 shadow-[0_40px_70px_-30px_rgba(0,0,0,0.65)] sm:p-6"
        style={{ background: 'linear-gradient(180deg, #2a3b31, #17221c)' }}
      >
        {mode === 'spread' ? (
          <div className="grid grid-cols-[1fr_auto_1fr] gap-0">
            <PagePanel {...panelProps('left')} />
            <Spine />
            <PagePanel {...panelProps('right')} />
          </div>
        ) : (
          <PagePanel {...panelProps(singleSide ?? 'left')} />
        )}
      </div>
    </DndContext>
  )
}
