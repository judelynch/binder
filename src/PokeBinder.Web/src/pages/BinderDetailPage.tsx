import { useEffect, useMemo, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { BinderFrame } from '../components/binder/BinderFrame'
import { BinderToolbar, type OwnedFilter } from '../components/binder/BinderToolbar'
import { MultiSelectToolbar } from '../components/binder/MultiSelectToolbar'
import { SlotActionPanel } from '../components/binder/SlotActionPanel'
import { SuggestionModal } from '../components/binder/SuggestionModal'
import { Modal } from '../components/Modal'
import { SearchSlideOver } from '../components/search/SearchSlideOver'
import { SetChecklistPanel } from '../components/search/SetChecklistPanel'
import { useBinder } from '../lib/queries/binders'
import { useOverlayTags } from '../lib/queries/overlay-tags'
import {
  useAppendPages,
  useBulkSetOwned,
  useBulkUnassignSlots,
  useMoveSlot,
  usePrices,
  useSpread,
  useSuggestions,
  useUnassignSlot,
  useUpdateSlotState,
} from '../lib/queries/spread'
import { clampPanelIndex, formatPanelLabel, formatSpreadLabel, panelIndexToLocation, totalPanels } from '../lib/panel-nav'
import { useIsMobile } from '../lib/useIsMobile'
import type { CardVariantPrice } from '../lib/pricing-types'
import type { BinderSlot, Spread, SlotSuggestions } from '../lib/spread-types'

/** Looks up a slot by id in the current spread, rather than trusting a snapshot taken when a
 * modal was opened - a snapshot goes stale the moment a mutation inside that modal updates the
 * spread query cache, so the modal would keep showing pre-mutation owned/tag/condition state. */
function findSlotInSpread(spread: Spread | undefined, slotId: string | null): BinderSlot | null {
  if (!spread || !slotId) return null
  const slots = [...(spread.leftPanel.slots ?? []), ...(spread.rightPanel.slots ?? [])]
  return slots.find((s) => s.slotId === slotId) ?? null
}

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
  const navigate = useNavigate()

  const [panelIndex, setPanelIndex] = useState(0)
  const [greyscaleEnabled, setGreyscaleEnabled] = useState(true)
  const [overlaysEnabled, setOverlaysEnabled] = useState(true)
  const [selectedSlotId, setSelectedSlotId] = useState<string | null>(null)
  const [addCardSlotId, setAddCardSlotId] = useState<string | null>(null)
  const [addingPages, setAddingPages] = useState(false)
  const [buildingSet, setBuildingSet] = useState(false)
  const [suggestingSlot, setSuggestingSlot] = useState<BinderSlot | null>(null)
  const [fullscreenEnabled, setFullscreenEnabled] = useState(false)
  const [selectMode, setSelectMode] = useState(false)
  const [selectedSlotIds, setSelectedSlotIds] = useState<Set<string>>(new Set())
  const [ownedFilter, setOwnedFilter] = useState<OwnedFilter>('all')
  const [visibleTagIds, setVisibleTagIds] = useState<Set<string> | null>(null)

  const spreadIndex = Math.floor(panelIndex / 2)
  const side = panelIndexToLocation(panelIndex).side

  const { data: binder } = useBinder(binderId)
  const { data: spread, isPending: spreadPending, isError: spreadError } = useSpread(binderId, spreadIndex)
  const { data: overlayTags } = useOverlayTags()
  const { data: suggestions } = useSuggestions(binderId, spreadIndex)
  const { data: priceSummary } = usePrices(binderId)
  const moveSlot = useMoveSlot(binderId, spreadIndex)
  const updateSlotState = useUpdateSlotState(binderId, spreadIndex)
  const unassignSlot = useUnassignSlot(binderId, spreadIndex)
  const bulkSetOwned = useBulkSetOwned(binderId)
  const bulkUnassign = useBulkUnassignSlots(binderId)

  const suggestionsBySlot = useMemo(() => {
    const map = new Map<string, SlotSuggestions>()
    suggestions?.forEach((s) => map.set(s.slotId, s))
    return map
  }, [suggestions])

  const priceByCardVariantId = useMemo(() => {
    const map = new Map<string, CardVariantPrice>()
    priceSummary?.prices.forEach((p) => map.set(p.cardVariantId, p))
    return map
  }, [priceSummary])

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
      if (e.key === 'Escape' && fullscreenEnabled) setFullscreenEnabled(false)
    }
    document.addEventListener('keydown', onKeyDown)
    return () => document.removeEventListener('keydown', onKeyDown)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [panelIndex, totalSpreads, isMobile, fullscreenEnabled])

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

  const selectedSlot = findSlotInSpread(spread, selectedSlotId)
  const completenessPercent = binder.filledSlots > 0 ? (binder.ownedCount / binder.filledSlots) * 100 : 0
  const positionLabel = isMobile
    ? formatPanelLabel(side === 'left' ? spread.leftPanel : spread.rightPanel, binder.pageCount, side)
    : formatSpreadLabel(spread.leftPanel, spread.rightPanel, binder.pageCount)

  function handleOpenSlot(slot: BinderSlot) {
    if (slot.card) {
      setSelectedSlotId(slot.slotId)
    } else {
      setAddCardSlotId(slot.slotId)
    }
  }

  function handleMoveSlot(sourceSlotId: string, targetSlotId: string) {
    moveSlot.mutate({ slotId: sourceSlotId, targetSlotId })
  }

  function handleToggleOwned(slot: BinderSlot) {
    updateSlotState.mutate({ slotId: slot.slotId, owned: !slot.owned })
  }

  function handleQuickRemove(slot: BinderSlot) {
    unassignSlot.mutate(slot.slotId)
  }

  function handleToggleSelectMode() {
    setSelectMode((v) => !v)
    setSelectedSlotIds(new Set())
  }

  function handleToggleSelect(slot: BinderSlot) {
    setSelectedSlotIds((prev) => {
      const next = new Set(prev)
      if (next.has(slot.slotId)) next.delete(slot.slotId)
      else next.add(slot.slotId)
      return next
    })
  }

  function handleBulkMarkOwned(owned: boolean) {
    bulkSetOwned.mutate({ slotIds: Array.from(selectedSlotIds), owned }, { onSuccess: () => setSelectedSlotIds(new Set()) })
  }

  function handleBulkRemove() {
    bulkUnassign.mutate(Array.from(selectedSlotIds), { onSuccess: () => setSelectedSlotIds(new Set()) })
  }

  function handleToggleTagVisibility(tagId: string) {
    setVisibleTagIds((prev) => {
      const allIds = new Set((overlayTags ?? []).map((t) => t.id))
      const current = prev ?? allIds
      const next = new Set(current)
      if (next.has(tagId)) next.delete(tagId)
      else next.add(tagId)
      return next.size === allIds.size ? null : next
    })
  }

  function isSlotDimmed(slot: BinderSlot): boolean {
    if (ownedFilter === 'owned' && !(slot.card && slot.owned)) return true
    if (ownedFilter === 'missing' && !(slot.card && !slot.owned)) return true
    if (visibleTagIds !== null && (!slot.overlayTag || !visibleTagIds.has(slot.overlayTag.id))) return true
    return false
  }

  const binderFrame = (
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
      suggestionsBySlot={suggestionsBySlot}
      onOpenSuggestions={setSuggestingSlot}
      onToggleOwned={handleToggleOwned}
      onQuickRemove={handleQuickRemove}
      selectMode={selectMode}
      selectedSlotIds={selectedSlotIds}
      onToggleSelect={handleToggleSelect}
      isSlotDimmed={isSlotDimmed}
      canGoPrev={canGoPrev}
      canGoNext={canGoNext}
      onNavigatePrev={goPrev}
      onNavigateNext={goNext}
      priceByCardVariantId={priceByCardVariantId}
    />
  )

  return (
    <div className={fullscreenEnabled ? 'fixed inset-0 z-40 overflow-y-auto bg-canvas p-4' : 'mx-auto max-w-4xl'}>
      <div className={fullscreenEnabled ? 'mx-auto max-w-5xl' : ''}>
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
          onBuildSet={() => setBuildingSet(true)}
          onOpenTableView={() => navigate(`/binders/${binderId}/table`)}
          overlayTags={overlayTags ?? []}
          fullscreenEnabled={fullscreenEnabled}
          onToggleFullscreen={() => setFullscreenEnabled((v) => !v)}
          selectMode={selectMode}
          onToggleSelectMode={handleToggleSelectMode}
          ownedFilter={ownedFilter}
          onOwnedFilterChange={setOwnedFilter}
          visibleTagIds={visibleTagIds}
          onToggleTagVisibility={handleToggleTagVisibility}
          onResetTagVisibility={() => setVisibleTagIds(null)}
          ownedValueGbp={priceSummary?.ownedValueGbp}
          missingCostGbp={priceSummary?.missingCostGbp}
        />
        {binderFrame}

        {selectMode && (
          <MultiSelectToolbar
            selectedCount={selectedSlotIds.size}
            onMarkOwned={() => handleBulkMarkOwned(true)}
            onMarkNotOwned={() => handleBulkMarkOwned(false)}
            onRemove={handleBulkRemove}
            onClear={() => setSelectedSlotIds(new Set())}
            isPending={bulkSetOwned.isPending || bulkUnassign.isPending}
          />
        )}
      </div>

      {selectedSlot && (
        <SlotActionPanel
          binderId={binderId}
          spreadIndex={spreadIndex}
          slot={selectedSlot}
          price={selectedSlot.cardVariantId ? priceByCardVariantId.get(selectedSlot.cardVariantId) : null}
          onClose={() => setSelectedSlotId(null)}
        />
      )}
      {addCardSlotId && (
        <SearchSlideOver binderId={binderId} startSlotId={addCardSlotId} onClose={() => setAddCardSlotId(null)} />
      )}
      {addingPages && <AddPagesModal binderId={binderId} onClose={() => setAddingPages(false)} />}
      {buildingSet && <SetChecklistPanel defaultBinderId={binderId} onClose={() => setBuildingSet(false)} />}
      {suggestingSlot && (
        <SuggestionModal
          slot={suggestingSlot}
          suggestions={suggestionsBySlot.get(suggestingSlot.slotId)?.suggestions ?? []}
          binderId={binderId}
          spreadIndex={spreadIndex}
          onClose={() => setSuggestingSlot(null)}
        />
      )}
    </div>
  )
}
