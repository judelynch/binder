import { useEffect, useState } from 'react'
import { useCardSearch, useSelectAllResults, SELECT_ALL_CAP } from '../../lib/queries/search'
import { capSelection, toggleSelection } from '../../lib/selection'
import { EMPTY_FILTERS, type CardSearchFilters, type CardSearchResult } from '../../lib/search-types'
import { InsertIntoBinderPanel } from './InsertIntoBinderPanel'
import { ResultsGrid } from './ResultsGrid'
import { SearchFilterPanel } from './SearchFilterPanel'
import { SelectionToolbar } from './SelectionToolbar'

export function CardSearchPanel({
  defaultBinderId,
  defaultStartSlotId,
  onOpenCard,
}: {
  defaultBinderId?: string
  defaultStartSlotId?: string
  onOpenCard?: (cardId: string) => void
}) {
  const [filters, setFilters] = useState<CardSearchFilters>(EMPTY_FILTERS)
  const [page, setPage] = useState(1)
  const [filtersOpen, setFiltersOpen] = useState(false)
  // A "selection" is now a specific card VARIANT (each variant renders as its own tile in
  // ResultsGrid), not a card with a separately-tracked variant choice.
  const [selectedIds, setSelectedIds] = useState<Set<string>>(new Set())
  const [cappedWarning, setCappedWarning] = useState<string | null>(null)
  const [insertOpen, setInsertOpen] = useState(false)

  const PAGE_SIZE = 60
  const search = useCardSearch(filters, page, PAGE_SIZE)
  const selectAll = useSelectAllResults(filters, false)
  const totalPages = search.data ? Math.max(1, Math.ceil(search.data.totalCount / PAGE_SIZE)) : 1

  // Selections persist across pages, but each page's results replace the last in `search.data` — so
  // without this, navigating away from a page silently drops any cards selected on it from the eventual
  // insert. Accumulate every card we've actually seen so resolveOrderedVariantIds can still find them.
  const [knownCards, setKnownCards] = useState<Map<string, CardSearchResult>>(new Map())
  useEffect(() => {
    if (!search.data) return
    setKnownCards((prev) => {
      const next = new Map(prev)
      for (const card of search.data!.items) next.set(card.id, card)
      return next
    })
  }, [search.data])

  function handleFiltersChange(next: CardSearchFilters) {
    setFilters(next)
    setPage(1)
    setKnownCards(new Map())
  }

  function toggleVariant(variantId: string) {
    setSelectedIds((prev) => toggleSelection(prev, variantId))
  }

  async function handleSelectAll() {
    const { data } = await selectAll.refetch()
    if (!data) return
    // Every variant of every matching card — a card with a Normal and a Reverse Holo selects both
    // tiles, in Normal-first order (guaranteed by the API), so a select-all-and-insert naturally adds
    // every printing of every matched card, not just one copy each.
    const { ids, wasCapped, totalAvailable } = capSelection(
      data.items.flatMap((c) => c.variants.map((v) => v.id)),
      SELECT_ALL_CAP,
    )
    setSelectedIds(new Set(ids))
    setCappedWarning(
      wasCapped ? `Only the first ${SELECT_ALL_CAP} of ${totalAvailable.toLocaleString()} matching variants were selected.` : null,
    )
  }

  // Insert in the current sort order — selectedIds is unordered, so re-derive order from the
  // last-known result set. "Select all" (which fetches every match in sort order in one shot) gives
  // an exact order when it was used; otherwise fall back to every card we've seen across pages visited
  // manually, in the order we saw them (a page-by-page approximation, but never silently drops a card).
  function resolveOrderedVariantIds(): string[] {
    const orderedCards = selectAll.data ? selectAll.data.items : Array.from(knownCards.values())
    const orderedVariantIds = orderedCards.flatMap((c) => c.variants.map((v) => v.id))
    return orderedVariantIds.filter((id) => selectedIds.has(id))
  }

  return (
    <div className="flex h-full flex-col md:flex-row">
      <div className="flex items-center justify-between border-b border-border p-3 md:hidden">
        <button
          type="button"
          onClick={() => setFiltersOpen(true)}
          className="rounded-lg border border-border px-3 py-1.5 text-xs font-semibold text-ink-soft"
        >
          Filters
        </button>
        <span className="text-xs text-ink-soft">{search.data?.totalCount.toLocaleString() ?? '…'} results</span>
      </div>

      <aside className="hidden w-72 shrink-0 border-r border-border md:block">
        <SearchFilterPanel filters={filters} onChange={handleFiltersChange} resultCount={search.data?.totalCount ?? null} />
      </aside>

      {filtersOpen && (
        <div className="fixed inset-0 z-50 md:hidden">
          <button aria-label="Close filters" className="absolute inset-0 bg-black/60" onClick={() => setFiltersOpen(false)} />
          <div className="absolute inset-y-0 left-0 w-80 max-w-[85vw] bg-surface">
            <SearchFilterPanel filters={filters} onChange={handleFiltersChange} resultCount={search.data?.totalCount ?? null} />
          </div>
        </div>
      )}

      <div className="flex min-w-0 flex-1 flex-col">
        <div className="min-h-0 flex-1 p-3">
          {search.isPending ? (
            <p className="p-4 text-sm text-ink-faint">Searching…</p>
          ) : search.data && search.data.items.length > 0 ? (
            <ResultsGrid results={search.data.items} selectedIds={selectedIds} onToggleSelect={toggleVariant} onOpenCard={onOpenCard} />
          ) : (
            <p className="p-4 text-sm text-ink-faint">No cards match these filters.</p>
          )}
        </div>

        {totalPages > 1 && (
          <div className="flex items-center justify-center gap-3 border-t border-border py-2 text-xs">
            <button
              type="button"
              disabled={page <= 1}
              onClick={() => setPage((p) => p - 1)}
              className="rounded border border-border px-2.5 py-1 text-ink-soft disabled:opacity-30"
            >
              Prev
            </button>
            <span className="text-ink-faint [font-variant-numeric:tabular-nums]">
              Page {page} of {totalPages}
            </span>
            <button
              type="button"
              disabled={page >= totalPages}
              onClick={() => setPage((p) => p + 1)}
              className="rounded border border-border px-2.5 py-1 text-ink-soft disabled:opacity-30"
            >
              Next
            </button>
          </div>
        )}

        <SelectionToolbar
          selectedCount={selectedIds.size}
          totalCount={search.data?.totalCount ?? 0}
          onSelectAll={handleSelectAll}
          onClear={() => {
            setSelectedIds(new Set())
            setCappedWarning(null)
          }}
          isSelectingAll={selectAll.isFetching}
          cappedWarning={cappedWarning}
          onInsert={() => setInsertOpen(true)}
        />
      </div>

      {insertOpen && (
        <InsertIntoBinderPanel
          cardVariantIds={resolveOrderedVariantIds()}
          defaultBinderId={defaultBinderId}
          defaultStartSlotId={defaultStartSlotId}
          onClose={() => setInsertOpen(false)}
          onInserted={() => {
            setSelectedIds(new Set())
            setCappedWarning(null)
          }}
        />
      )}
    </div>
  )
}
