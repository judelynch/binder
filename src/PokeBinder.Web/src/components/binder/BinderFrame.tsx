import { DndContext, DragOverlay, PointerSensor, useSensor, useSensors, type DragStartEvent, type DragEndEvent } from '@dnd-kit/core'
import { useEffect, useRef, useState } from 'react'
import { resolveDragMove } from '../../lib/dnd'
import type { PanelSide } from '../../lib/panel-nav'
import type { CardVariantPrice } from '../../lib/pricing-types'
import type { BinderSlot, SlotSuggestions, Spread } from '../../lib/spread-types'
import { CardImage } from './CardImage'
import { PagePanel } from './PagePanel'
import { Spine } from './Spine'

const EDGE_ZONE_PX = 56
const EDGE_HOLD_MS = 650

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
  suggestionsBySlot,
  onOpenSuggestions,
  onToggleOwned,
  onQuickRemove,
  selectMode,
  selectedSlotIds,
  onToggleSelect,
  isSlotDimmed,
  canGoPrev,
  canGoNext,
  onNavigatePrev,
  onNavigateNext,
  priceByCardVariantId,
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
  suggestionsBySlot?: Map<string, SlotSuggestions>
  onOpenSuggestions?: (slot: BinderSlot) => void
  onToggleOwned?: (slot: BinderSlot) => void
  onQuickRemove?: (slot: BinderSlot) => void
  selectMode?: boolean
  selectedSlotIds?: ReadonlySet<string>
  onToggleSelect?: (slot: BinderSlot) => void
  isSlotDimmed?: (slot: BinderSlot) => boolean
  canGoPrev?: boolean
  canGoNext?: boolean
  onNavigatePrev?: () => void
  onNavigateNext?: () => void
  priceByCardVariantId?: Map<string, CardVariantPrice>
}) {
  const [activeSlot, setActiveSlot] = useState<BinderSlot | null>(null)

  // Kept in refs (not state) so the pointermove listener below always reads the latest values
  // without needing to be torn down and re-attached on every render.
  const navRef = useRef({ canGoPrev, canGoNext, onNavigatePrev, onNavigateNext })
  navRef.current = { canGoPrev, canGoNext, onNavigatePrev, onNavigateNext }

  function findSlot(slotId: string | number): BinderSlot | null {
    const all = [...(spread.leftPanel.slots ?? []), ...(spread.rightPanel.slots ?? [])]
    return all.find((s) => s.slotId === String(slotId)) ?? null
  }

  function handleDragStart(event: DragStartEvent) {
    setActiveSlot(findSlot(event.active.id))
  }

  function handleDragEnd(event: DragEndEvent) {
    setActiveSlot(null)
    const move = resolveDragMove(event)
    if (move) onMoveSlot(move.sourceSlotId, move.targetSlotId)
  }

  // Dragging a card near the left/right edge of the viewport auto-advances the spread after a
  // short hold, so a card on page 1 can be dragged all the way to page 10 without needing the
  // target page to already be on screen. Uses a DragOverlay (below) so the dragged card's visual
  // survives the source slot unmounting when the page turns underneath it.
  useEffect(() => {
    if (!activeSlot) return

    let edgeTimer: number | null = null
    let armedEdge: 'left' | 'right' | null = null

    function clearArmedEdge() {
      if (edgeTimer !== null) {
        window.clearTimeout(edgeTimer)
        edgeTimer = null
      }
      armedEdge = null
    }

    function onPointerMove(e: PointerEvent) {
      const nearLeft = e.clientX < EDGE_ZONE_PX
      const nearRight = e.clientX > window.innerWidth - EDGE_ZONE_PX
      const edge = nearLeft ? 'left' : nearRight ? 'right' : null

      if (edge === armedEdge) {
        return
      }
      clearArmedEdge()

      if (edge === null) {
        return
      }

      armedEdge = edge
      edgeTimer = window.setTimeout(() => {
        const nav = navRef.current
        if (edge === 'left' && nav.canGoPrev) nav.onNavigatePrev?.()
        if (edge === 'right' && nav.canGoNext) nav.onNavigateNext?.()
        armedEdge = null
      }, EDGE_HOLD_MS)
    }

    window.addEventListener('pointermove', onPointerMove)
    return () => {
      window.removeEventListener('pointermove', onPointerMove)
      clearArmedEdge()
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [activeSlot])

  // Without an activation distance, dnd-kit's PointerSensor treats any pointerdown on a draggable
  // slot as the start of a drag — even the sub-pixel jitter of a normal click — which swallows the
  // click event entirely and makes filled slots unopenable. Requiring a few pixels of movement lets
  // a real click through while still recognizing an intentional drag.
  const sensors = useSensors(useSensor(PointerSensor, { activationConstraint: { distance: 8 } }))

  const panelProps = (side: PanelSide) => ({
    panel: side === 'left' ? spread.leftPanel : spread.rightPanel,
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
    priceByCardVariantId,
  })

  return (
    <DndContext sensors={sensors} onDragStart={handleDragStart} onDragEnd={handleDragEnd} onDragCancel={() => setActiveSlot(null)}>
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

      <DragOverlay>
        {activeSlot?.card && (
          <div className="relative aspect-[5/7] w-20 overflow-hidden rounded-lg shadow-2xl ring-2 ring-accent">
            <CardImage src={activeSlot.card.imageSmallUrl} alt={activeSlot.card.name} greyscale={false} />
          </div>
        )}
      </DragOverlay>
    </DndContext>
  )
}
