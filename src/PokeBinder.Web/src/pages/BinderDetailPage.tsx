import { useEffect, useState } from 'react'
import { useParams } from 'react-router-dom'
import { AddCardPanel } from '../components/binder/AddCardPanel'
import { BinderFrame } from '../components/binder/BinderFrame'
import { BinderToolbar } from '../components/binder/BinderToolbar'
import { SlotActionPanel } from '../components/binder/SlotActionPanel'
import { Modal } from '../components/Modal'
import { useBinder } from '../lib/queries/binders'
import { useOverlayTags } from '../lib/queries/overlay-tags'
import { useAppendPages, useMoveSlot, useSpread } from '../lib/queries/spread'
import { clampPanelIndex, formatPanelLabel, formatSpreadLabel, panelIndexToLocation, totalPanels } from '../lib/panel-nav'
import { useIsMobile } from '../lib/useIsMobile'
import type { BinderSlot } from '../lib/spread-types'

function AddPagesModal({ binderId, onClose }: { binderId: string; onClose: () => void }) {
  const [count, setCount] = useState(2)
  const appendPages = useAppendPages(binderId)

  return (
    <Modal title="Add pages" onClose={onClose}>
      <div className="flex items-center justify-center gap-3">
        <button
          type="button"
          aria-label="Decrease"
          onClick={() => setCount((c) => Math.max(2, c - 2))}
          className="flex h-9 w-9 items-center justify-center rounded-lg border border-border text-ink-soft"
        >
          −
        </button>
        <span className="w-16 text-center text-lg font-semibold text-ink [font-variant-numeric:tabular-nums]">
          {count}
        </span>
        <button
          type="button"
          aria-label="Increase"
          onClick={() => setCount((c) => c + 2)}
          className="flex h-9 w-9 items-center justify-center rounded-lg border border-border text-ink-soft"
        >
          +
        </button>
      </div>
      <p className="mt-2 text-center text-xs text-ink-faint">Pages are always added in pairs.</p>
      <button
        type="button"
        disabled={appendPages.isPending}
        onClick={() => appendPages.mutate(count, { onSuccess: onClose })}
        className="mt-5 w-full rounded-lg bg-accent py-2 text-sm font-semibold text-accent-ink disabled:opacity-50"
      >
        {appendPages.isPending ? 'Adding…' : `Add ${count} pages`}
      </button>
    </Modal>
  )
}

export function BinderDetailPage() {
  const { id } = useParams<{ id: string }>()
  const binderId = id!
  const isMobile = useIsMobile()

  const [panelIndex, setPanelIndex] = useState(0)
  const [greyscaleEnabled, setGreyscaleEnabled] = useState(true)
  const [overlaysEnabled, setOverlaysEnabled] = useState(true)
  const [selectedSlot, setSelectedSlot] = useState<BinderSlot | null>(null)
  const [addCardSlotId, setAddCardSlotId] = useState<string | null>(null)
  const [addingPages, setAddingPages] = useState(false)

  const spreadIndex = Math.floor(panelIndex / 2)
  const side = panelIndexToLocation(panelIndex).side

  const { data: binder } = useBinder(binderId)
  const { data: spread, isPending: spreadPending, isError: spreadError } = useSpread(binderId, spreadIndex)
  const { data: overlayTags } = useOverlayTags()
  const moveSlot = useMoveSlot(binderId, spreadIndex)

  const totalSpreads = spread?.totalSpreads ?? 0

  useEffect(() => {
    if (spread) {
      setPanelIndex((current) => clampPanelIndex(current, totalSpreads))
    }
  }, [spread, totalSpreads])

  const step = isMobile ? 1 : 2
  const canGoPrev = panelIndex - step >= 0
  const canGoNext = panelIndex + step <= totalPanels(totalSpreads) - 1

  function goPrev() {
    setPanelIndex((current) => clampPanelIndex(current - step, totalSpreads))
  }
  function goNext() {
    setPanelIndex((current) => clampPanelIndex(current + step, totalSpreads))
  }

  useEffect(() => {
    function onKeyDown(e: KeyboardEvent) {
      const tag = (document.activeElement?.tagName ?? '').toLowerCase()
      if (tag === 'input' || tag === 'textarea' || tag === 'select') return
      if (e.key === 'ArrowLeft') goPrev()
      if (e.key === 'ArrowRight') goNext()
    }
    document.addEventListener('keydown', onKeyDown)
    return () => document.removeEventListener('keydown', onKeyDown)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [panelIndex, totalSpreads, isMobile])

  if (!binder || spreadPending) {
    return (
      <div className="mx-auto max-w-4xl animate-pulse">
        <div className="h-16 rounded-t-2xl bg-surface" />
        <div className="h-96 rounded-b-2xl bg-surface-2" />
      </div>
    )
  }

  if (spreadError || !spread) {
    return <p className="text-sm text-bad">Couldn't load this binder. Try refreshing.</p>
  }

  const completenessPercent = binder.filledSlots > 0 ? (binder.ownedCount / binder.filledSlots) * 100 : 0
  const positionLabel = isMobile
    ? formatPanelLabel(side === 'left' ? spread.leftPanel : spread.rightPanel, binder.pageCount, side)
    : formatSpreadLabel(spread.leftPanel, spread.rightPanel, binder.pageCount)

  function handleOpenSlot(slot: BinderSlot) {
    if (slot.card) {
      setSelectedSlot(slot)
    } else {
      setAddCardSlotId(slot.slotId)
    }
  }

  function handleMoveSlot(sourceSlotId: string, targetSlotId: string) {
    moveSlot.mutate({ slotId: sourceSlotId, targetSlotId })
  }

  return (
    <div className="mx-auto max-w-4xl">
      <BinderToolbar
        binderName={binder.name}
        completenessPercent={completenessPercent}
        positionLabel={positionLabel}
        onPrev={goPrev}
        onNext={goNext}
        canGoPrev={canGoPrev}
        canGoNext={canGoNext}
        greyscaleEnabled={greyscaleEnabled}
        onToggleGreyscale={() => setGreyscaleEnabled((v) => !v)}
        overlaysEnabled={overlaysEnabled}
        onToggleOverlays={() => setOverlaysEnabled((v) => !v)}
        onAddPages={() => setAddingPages(true)}
        overlayTags={overlayTags ?? []}
      />
      <BinderFrame
        spread={spread}
        mode={isMobile ? 'single' : 'spread'}
        singleSide={side}
        binderColourHex={binder.colourHex}
        columns={binder.columns}
        greyscaleEnabled={greyscaleEnabled}
        overlaysEnabled={overlaysEnabled}
        onOpenSlot={handleOpenSlot}
        onMoveSlot={handleMoveSlot}
      />

      {selectedSlot && (
        <SlotActionPanel
          binderId={binderId}
          spreadIndex={spreadIndex}
          slot={selectedSlot}
          onClose={() => setSelectedSlot(null)}
        />
      )}
      {addCardSlotId && (
        <AddCardPanel
          binderId={binderId}
          spreadIndex={spreadIndex}
          slotId={addCardSlotId}
          onClose={() => setAddCardSlotId(null)}
        />
      )}
      {addingPages && <AddPagesModal binderId={binderId} onClose={() => setAddingPages(false)} />}
    </div>
  )
}
