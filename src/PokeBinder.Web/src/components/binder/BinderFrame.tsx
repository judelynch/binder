import { DndContext, DragOverlay, PointerSensor, useSensor, useSensors, type DragStartEvent, type DragEndEvent } from '@dnd-kit/core'
import { useEffect, useRef, useState } from 'react'
import { resolveDragMove } from '../../lib/dnd'
import type { PanelSide } from '../../lib/panel-nav'
import type { CardVariantPrice } from '../../lib/pricing-types'
import type { BinderSlot, EmptySlotSuggestions, Spread } from '../../lib/spread-types'
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
  onBulkMoveSlots,
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
  onBulkMoveSlots?: (sourceSlotIds: string[], startSlotId: string) => void
  suggestionsBySlot?: Map<string, EmptySlotSuggestions>
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
  const [hoverEdge, setHoverEdge] = useState<'left' | 'right' | null>(null)
  // Bumped every time an edge is (re-)armed, so the progress-bar div below can use it as a `key`
  // to restart its fill animation from zero on each hold rather than continuing a stale one.
  const [progressKey, setProgressKey] = useState(0)

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
    if (!move) return

    // Dragging a card that's part of the current multi-selection moves the whole selection
    // together, dropped starting at wherever this one card landed. Dragging a card that ISN'T
    // selected (even with select mode on) just moves that one card, same as normal.
    if (selectMode && onBulkMoveSlots && selectedSlotIds?.has(move.sourceSlotId) && selectedSlotIds.size > 1) {
      onBulkMoveSlots(Array.from(selectedSlotIds), move.targetSlotId)
      return
    }

    onMoveSlot(move.sourceSlotId, move.targetSlotId)
  }

  // Dragging a card near the left/right edge of the viewport auto-advances the spread after a
  // short hold, so a card on page 1 can be dragged all the way to page 16 without needing the
  // target page to already be on screen — hold there and it keeps turning, one page per
  // EDGE_HOLD_MS, until released, dropped, or the end of the binder. Uses a DragOverlay (below)
  // so the dragged card's visual survives the source slot unmounting as pages turn underneath it.
  useEffect(() => {
    if (!activeSlot) {
      setHoverEdge(null)
      return
    }

    let edgeTimer: number | null = null
    let armedEdge: 'left' | 'right' | null = null

    function clearArmedEdge() {
      if (edgeTimer !== null) {
        window.clearTimeout(edgeTimer)
        edgeTimer = null
      }
      armedEdge = null
      setHoverEdge(null)
    }

    function armEdge(edge: 'left' | 'right') {
      armedEdge = edge
      setHoverEdge(edge)
      setProgressKey((k) => k + 1)
      edgeTimer = window.setTimeout(() => {
        const nav = navRef.current
        const canContinue = edge === 'left' ? nav.canGoPrev : nav.canGoNext
        if (!canContinue) {
          armedEdge = null
          setHoverEdge(null)
          return
        }
        if (edge === 'left') nav.onNavigatePrev?.()
        else nav.onNavigateNext?.()
        armEdge(edge) // still held - keep turning pages until released or the binder ends
      }, EDGE_HOLD_MS)
    }

    function onPointerMove(e: PointerEvent) {
      const nearLeft = e.clientX < EDGE_ZONE_PX
      const nearRight = e.clientX > window.innerWidth - EDGE_ZONE_PX
      const edge = nearLeft ? 'left' : nearRight ? 'right' : null

      if (edge === armedEdge) {
        return
      }
      clearArmedEdge()

      if (edge !== null) {
        armEdge(edge)
      }
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

      {activeSlot && (
        <>
          <EdgeTurnPanel side="left" active={hoverEdge === 'left'} enabled={!!canGoPrev} progressKey={progressKey} />
          <EdgeTurnPanel side="right" active={hoverEdge === 'right'} enabled={!!canGoNext} progressKey={progressKey} />
        </>
      )}
    </DndContext>
  )
}

/** The visible drop-here-to-turn-pages strip shown along each edge of the viewport while a card is
 * being dragged. Purely decorative/informational — pointer-events-none so it never interferes with
 * dnd-kit's own hit-testing — the actual hold-to-turn logic lives in BinderFrame's pointermove effect. */
function EdgeTurnPanel({
  side,
  active,
  enabled,
  progressKey,
}: {
  side: 'left' | 'right'
  active: boolean
  enabled: boolean
  progressKey: number
}) {
  const sideClasses = side === 'left' ? 'left-0 border-r' : 'right-0 border-l'
  return (
    <div
      className={`pointer-events-none fixed inset-y-0 z-50 flex w-14 flex-col items-center justify-center gap-3 ${sideClasses} transition-colors ${
        active ? 'border-accent bg-accent/15' : 'border-border/60 bg-canvas/80'
      } ${enabled ? '' : 'opacity-30'}`}
    >
      <span
        className={`font-display text-3xl text-accent transition-transform ${
          active ? (side === 'left' ? '-translate-x-1' : 'translate-x-1') : ''
        }`}
      >
        {side === 'left' ? '‹' : '›'}
      </span>
      <div className="h-16 w-1 overflow-hidden rounded-full bg-border/60">
        {active && <div key={progressKey} className="edge-page-turn-progress h-full w-full origin-bottom bg-accent" />}
      </div>
    </div>
  )
}
