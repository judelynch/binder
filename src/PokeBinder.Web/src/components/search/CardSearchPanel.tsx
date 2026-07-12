import { useState } from 'react'
import { useCardSearch, useSelectAllResults, SELECT_ALL_CAP } from '../../lib/queries/search'
import { capSelection, toggleSelection } from '../../lib/selection'
import { EMPTY_FILTERS, type CardSearchFilters } from '../../lib/search-types'
import { InsertIntoBinderPanel } from './InsertIntoBinderPanel'
import { ResultsGrid } from './ResultsGrid'
import { SearchFilterPanel } from './SearchFilterPanel'
import { SelectionToolbar } from './SelectionToolbar'

export function CardSearchPanel({
  defaultBinderId,
  defaultStartSlotId,
}: {
  defaultBinderId?: string
  defaultStartSlotId?: string
}) {
  const [filters, setFilters] = useState<CardSearchFilters>(EMPTY_FILTERS)
  const [filtersOpen, setFiltersOpen] = useState(false)
  const [selectedIds, setSelectedIds] = useState<Set<string>>(new Set())
  const [selectedVariants, setSelectedVariants] = useState<Record<string, string>>({})
  const [cappedWarning, setCappedWarning] = useState<string | null>(null)
  const [insertOpen, setInsertOpen] = useState(false)

  const search = useCardSearch(filters, 1, 60)
  const selectAll = useSelectAllResults(filters, false)

  function toggleCard(id: string) {
    setSelectedIds((prev) => toggleSelection(prev, id))
  }

  async function handleSelectAll() {
    const { data } = await selectAll.refetch()
    if (!data) return
    const { ids, wasCapped, totalAvailable } = capSelection(
      data.items.map((c) => c.id),
      SELECT_ALL_CAP,
    )
    setSelectedIds(new Set(ids))
    setCappedWarning(
      wasCapped ? `Only the first ${SELECT_ALL_CAP} of ${totalAvailable.toLocaleString()} matching cards were selected.` : null,
    )
  }

  // Cards insert in the current sort order — selectedIds is unordered, so re-derive order from the
  // last-known result set (search results + the select-all fetch, whichever has the card).
  function resolveOrderedVariantIds(): string[] {
    const bySearch = new Map(search.data?.items.map((c) => [c.id, c]) ?? [])
    const bySelectAll = new Map(selectAll.data?.items.map((c) => [c.id, c]) ?? [])
    const ordered = (selectAll.data?.items ?? search.data?.items ?? []).filter((c) => selectedIds.has(c.id))
    return ordered.map((c) => {
      const card = bySelectAll.get(c.id) ?? bySearch.get(c.id)!
      return selectedVariants[c.id] ?? card.variants[0]?.id
    }).filter((id): id is string => !!id)
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
        <SearchFilterPanel filters={filters} onChange={setFilters} resultCount={search.data?.totalCount ?? null} />
      </aside>

      {filtersOpen && (
        <div className="fixed inset-0 z-50 md:hidden">
          <button aria-label="Close filters" className="absolute inset-0 bg-black/60" onClick={() => setFiltersOpen(false)} />
          <div className="absolute inset-y-0 left-0 w-80 max-w-[85vw] bg-surface">
            <SearchFilterPanel filters={filters} onChange={setFilters} resultCount={search.data?.totalCount ?? null} />
          </div>
        </div>
      )}

      <div className="flex min-w-0 flex-1 flex-col">
        <div className="min-h-0 flex-1 p-3">
          {search.isPending ? (
            <p className="p-4 text-sm text-ink-faint">Searching…</p>
          ) : search.data && search.data.items.length > 0 ? (
            <ResultsGrid
              results={search.data.items}
              selectedIds={selectedIds}
              onToggleSelect={toggleCard}
              selectedVariants={selectedVariants}
              onSelectVariant={(cardId, variantId) => setSelectedVariants((prev) => ({ ...prev, [cardId]: variantId }))}
            />
          ) : (
            <p className="p-4 text-sm text-ink-faint">No cards match these filters.</p>
          )}
        </div>

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
