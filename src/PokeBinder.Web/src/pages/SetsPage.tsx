import { useMemo, useState } from 'react'
import { SetTile } from '../components/sets/SetTile'
import { EmptyState } from '../components/EmptyState'
import { Skeleton } from '../components/Skeleton'
import { EMPTY_SET_FILTERS, filterAndSortSets, uniqueSeries, type SetFilters, type SetSortOrder } from '../lib/filterSets'
import { useSets } from '../lib/queries/cards'

const SORT_LABELS: Record<SetSortOrder, string> = {
  releaseDateDesc: 'Newest first',
  releaseDateAsc: 'Oldest first',
  nameAsc: 'Name A–Z',
  completionDesc: 'Most complete',
}

export function SetsPage() {
  const { data: sets, isPending, isError } = useSets()
  const [filters, setFilters] = useState<SetFilters>(EMPTY_SET_FILTERS)

  const series = useMemo(() => uniqueSeries(sets ?? []), [sets])
  const visibleSets = useMemo(() => filterAndSortSets(sets ?? [], filters), [sets, filters])

  return (
    <div>
      <div>
        <h1 className="font-display text-2xl font-semibold italic text-ink">Sets</h1>
        <p className="mt-1 text-sm text-ink-soft">Browse the catalog by set and track how much of each one you own.</p>
      </div>

      <div className="mt-5 flex flex-wrap items-center gap-3">
        <input
          type="text"
          value={filters.query}
          onChange={(e) => setFilters((f) => ({ ...f, query: e.target.value }))}
          placeholder="Search sets…"
          className="w-full max-w-xs rounded-lg border border-border bg-surface px-3 py-2 text-sm text-ink placeholder:text-ink-faint"
        />
        <select
          value={filters.series ?? ''}
          onChange={(e) => setFilters((f) => ({ ...f, series: e.target.value || null }))}
          className="rounded-lg border border-border bg-surface px-3 py-2 text-sm text-ink"
        >
          <option value="">All series</option>
          {series.map((s) => (
            <option key={s} value={s}>
              {s}
            </option>
          ))}
        </select>
        <select
          value={filters.sort}
          onChange={(e) => setFilters((f) => ({ ...f, sort: e.target.value as SetSortOrder }))}
          className="rounded-lg border border-border bg-surface px-3 py-2 text-sm text-ink"
        >
          {(Object.keys(SORT_LABELS) as SetSortOrder[]).map((key) => (
            <option key={key} value={key}>
              {SORT_LABELS[key]}
            </option>
          ))}
        </select>
      </div>

      <div className="mt-6">
        {isPending ? (
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
            {Array.from({ length: 8 }).map((_, i) => (
              <Skeleton key={i} className="h-32 rounded-xl" />
            ))}
          </div>
        ) : isError ? (
          <p className="text-sm text-bad">Couldn't load sets. Try refreshing.</p>
        ) : visibleSets.length === 0 ? (
          <EmptyState title="No sets match" message="Try a different search or clear the series filter." />
        ) : (
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
            {visibleSets.map((set) => (
              <SetTile key={set.id} set={set} />
            ))}
          </div>
        )}
      </div>
    </div>
  )
}
