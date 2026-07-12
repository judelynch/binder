export function SelectionToolbar({
  selectedCount,
  totalCount,
  onSelectAll,
  onClear,
  isSelectingAll,
  cappedWarning,
  onInsert,
}: {
  selectedCount: number
  totalCount: number
  onSelectAll: () => void
  onClear: () => void
  isSelectingAll: boolean
  cappedWarning: string | null
  onInsert?: () => void
}) {
  return (
    <div className="flex flex-wrap items-center justify-between gap-2 border-t border-border bg-surface px-4 py-3">
      <div className="flex items-center gap-3">
        <span className="text-sm font-semibold text-ink">
          {selectedCount > 0 ? `${selectedCount.toLocaleString()} selected` : 'Nothing selected'}
        </span>
        <button type="button" onClick={onSelectAll} disabled={isSelectingAll || totalCount === 0} className="text-xs font-semibold text-accent disabled:opacity-40">
          {isSelectingAll ? 'Loading…' : `Select all ${totalCount.toLocaleString()} results`}
        </button>
        {selectedCount > 0 && (
          <button type="button" onClick={onClear} className="text-xs font-semibold text-ink-soft">
            Clear
          </button>
        )}
      </div>
      {onInsert && (
        <button
          type="button"
          onClick={onInsert}
          disabled={selectedCount === 0}
          className="rounded-lg bg-accent px-3 py-1.5 text-xs font-semibold text-accent-ink disabled:opacity-40"
        >
          Insert into binder →
        </button>
      )}
      {cappedWarning && <p className="w-full text-xs text-bad">{cappedWarning}</p>}
    </div>
  )
}
