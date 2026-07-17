export function MultiSelectToolbar({
  selectedCount,
  onMarkOwned,
  onMarkNotOwned,
  onRemove,
  onClear,
  isPending,
}: {
  selectedCount: number
  onMarkOwned: () => void
  onMarkNotOwned: () => void
  onRemove: () => void
  onClear: () => void
  isPending: boolean
}) {
  return (
    <div className="sticky bottom-0 z-10 mt-3 flex flex-wrap items-center justify-between gap-2 rounded-xl border border-border bg-surface px-4 py-3 shadow-lg">
      <div className="flex items-center gap-3">
        <span className="text-sm font-semibold text-ink">
          {selectedCount > 0 ? `${selectedCount.toLocaleString()} selected` : 'Tap slots to select'}
        </span>
        {selectedCount > 0 && (
          <button type="button" onClick={onClear} className="text-xs font-semibold text-ink-soft">
            Clear
          </button>
        )}
      </div>
      <div className="flex flex-wrap items-center gap-2">
        <button
          type="button"
          disabled={selectedCount === 0 || isPending}
          onClick={onMarkOwned}
          className="rounded-lg border border-border px-3 py-1.5 text-xs font-semibold text-ink-soft hover:text-ink disabled:opacity-40"
        >
          Mark owned
        </button>
        <button
          type="button"
          disabled={selectedCount === 0 || isPending}
          onClick={onMarkNotOwned}
          className="rounded-lg border border-border px-3 py-1.5 text-xs font-semibold text-ink-soft hover:text-ink disabled:opacity-40"
        >
          Mark not owned
        </button>
        <button
          type="button"
          disabled={selectedCount === 0 || isPending}
          onClick={onRemove}
          className="rounded-lg border border-bad/50 px-3 py-1.5 text-xs font-semibold text-bad disabled:opacity-40"
        >
          Remove
        </button>
      </div>
    </div>
  )
}
