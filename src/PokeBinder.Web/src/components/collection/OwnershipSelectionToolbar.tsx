export function OwnershipSelectionToolbar({
  selectedCount,
  totalCount,
  onSelectAll,
  onClear,
  onMarkOwned,
  onMarkUnowned,
  isMutating,
}: {
  selectedCount: number
  totalCount: number
  onSelectAll: () => void
  onClear: () => void
  onMarkOwned: () => void
  onMarkUnowned: () => void
  isMutating: boolean
}) {
  return (
    <div className="flex flex-wrap items-center justify-between gap-2 border-t border-border bg-surface px-4 py-3">
      <div className="flex items-center gap-3">
        <span className="text-sm font-semibold text-ink">
          {selectedCount > 0 ? `${selectedCount.toLocaleString()} selected` : 'Nothing selected'}
        </span>
        <button type="button" onClick={onSelectAll} disabled={totalCount === 0} className="text-xs font-semibold text-accent disabled:opacity-40">
          Select all {totalCount.toLocaleString()}
        </button>
        {selectedCount > 0 && (
          <button type="button" onClick={onClear} className="text-xs font-semibold text-ink-soft">
            Clear
          </button>
        )}
      </div>
      <div className="flex gap-2">
        <button
          type="button"
          onClick={onMarkUnowned}
          disabled={selectedCount === 0 || isMutating}
          className="rounded-lg border border-border px-3 py-1.5 text-xs font-semibold text-ink-soft disabled:opacity-40"
        >
          Mark as not owned
        </button>
        <button
          type="button"
          onClick={onMarkOwned}
          disabled={selectedCount === 0 || isMutating}
          className="rounded-lg bg-accent px-3 py-1.5 text-xs font-semibold text-accent-ink disabled:opacity-40"
        >
          Mark as owned
        </button>
      </div>
    </div>
  )
}
