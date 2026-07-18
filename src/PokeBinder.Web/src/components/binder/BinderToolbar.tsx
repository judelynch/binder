import type { OverlayTag } from '../../lib/spread-types'

export type OwnedFilter = 'all' | 'owned' | 'missing'

export function BinderToolbar({
  binderName,
  completenessPercent,
  positionLabel,
  onPrev,
  onNext,
  canGoPrev,
  canGoNext,
  greyscaleEnabled,
  onToggleGreyscale,
  overlaysEnabled,
  onToggleOverlays,
  onAddPages,
  onBuildSet,
  onOpenTableView,
  overlayTags,
  fullscreenEnabled,
  onToggleFullscreen,
  selectMode,
  onToggleSelectMode,
  ownedFilter,
  onOwnedFilterChange,
  visibleTagIds,
  onToggleTagVisibility,
  onResetTagVisibility,
  ownedValueGbp,
  missingCostGbp,
}: {
  binderName: string
  completenessPercent: number
  positionLabel: string
  onPrev: () => void
  onNext: () => void
  canGoPrev: boolean
  canGoNext: boolean
  greyscaleEnabled: boolean
  onToggleGreyscale: () => void
  overlaysEnabled: boolean
  onToggleOverlays: () => void
  onAddPages: () => void
  onBuildSet: () => void
  onOpenTableView: () => void
  overlayTags: OverlayTag[]
  fullscreenEnabled: boolean
  onToggleFullscreen: () => void
  selectMode: boolean
  onToggleSelectMode: () => void
  ownedFilter: OwnedFilter
  onOwnedFilterChange: (filter: OwnedFilter) => void
  visibleTagIds: ReadonlySet<string> | null
  onToggleTagVisibility: (tagId: string) => void
  onResetTagVisibility: () => void
  ownedValueGbp?: number | null
  missingCostGbp?: number | null
}) {
  const tagsAreFiltered = visibleTagIds !== null

  return (
    <div className="rounded-t-[14px] border border-border bg-surface p-3.5 sm:p-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div className="flex min-w-0 items-center gap-3">
          <div className="truncate font-display text-base italic text-ink sm:text-lg">{binderName}</div>
          <div className="flex items-center gap-2">
            <div className="h-1.5 w-16 overflow-hidden rounded-full bg-border sm:w-24">
              <div className="h-full rounded-full bg-accent" style={{ width: `${Math.max(0, Math.min(100, completenessPercent))}%` }} />
            </div>
            <span className="text-[11px] text-ink-soft [font-variant-numeric:tabular-nums]">
              {Math.round(completenessPercent)}%
            </span>
          </div>
          {(ownedValueGbp != null || missingCostGbp != null) && (
            <div className="hidden items-center gap-2.5 text-[11px] sm:flex">
              {ownedValueGbp != null && (
                <span className="text-good [font-variant-numeric:tabular-nums]">
                  £{ownedValueGbp.toFixed(2)} <span className="text-ink-faint">owned</span>
                </span>
              )}
              {missingCostGbp != null && (
                <span className="text-ink-soft [font-variant-numeric:tabular-nums]">
                  £{missingCostGbp.toFixed(2)} <span className="text-ink-faint">to complete</span>
                </span>
              )}
            </div>
          )}
        </div>

        <div className="flex flex-wrap items-center gap-2">
          <div className="flex items-center gap-1.5">
            <button
              type="button"
              aria-label="Previous"
              disabled={!canGoPrev}
              onClick={onPrev}
              className="flex h-8 w-8 items-center justify-center rounded-lg border border-border text-ink-soft disabled:opacity-30"
            >
              ‹
            </button>
            <span className="min-w-max text-xs font-semibold text-ink-soft">{positionLabel}</span>
            <button
              type="button"
              aria-label="Next"
              disabled={!canGoNext}
              onClick={onNext}
              className="flex h-8 w-8 items-center justify-center rounded-lg border border-border text-ink-soft disabled:opacity-30"
            >
              ›
            </button>
          </div>

          <button
            type="button"
            onClick={onToggleGreyscale}
            aria-pressed={greyscaleEnabled}
            className={`rounded-lg border px-2.5 py-1.5 text-[11px] font-semibold ${
              greyscaleEnabled ? 'border-accent text-accent' : 'border-border text-ink-soft'
            }`}
          >
            Greyscale
          </button>
          <button
            type="button"
            onClick={onToggleOverlays}
            aria-pressed={overlaysEnabled}
            className={`rounded-lg border px-2.5 py-1.5 text-[11px] font-semibold ${
              overlaysEnabled ? 'border-accent text-accent' : 'border-border text-ink-soft'
            }`}
          >
            Overlays
          </button>
          <button
            type="button"
            onClick={onToggleSelectMode}
            aria-pressed={selectMode}
            className={`rounded-lg border px-2.5 py-1.5 text-[11px] font-semibold ${
              selectMode ? 'border-accent text-accent' : 'border-border text-ink-soft'
            }`}
          >
            Select
          </button>
          <button
            type="button"
            onClick={onToggleFullscreen}
            aria-pressed={fullscreenEnabled}
            className={`rounded-lg border px-2.5 py-1.5 text-[11px] font-semibold ${
              fullscreenEnabled ? 'border-accent text-accent' : 'border-border text-ink-soft'
            }`}
          >
            {fullscreenEnabled ? 'Exit full screen' : 'Full screen'}
          </button>
          <button
            type="button"
            onClick={onAddPages}
            className="rounded-lg bg-accent px-2.5 py-1.5 text-[11px] font-semibold text-accent-ink"
          >
            + Add pages
          </button>
          <button
            type="button"
            onClick={onBuildSet}
            className="rounded-lg border border-border px-2.5 py-1.5 text-[11px] font-semibold text-ink-soft hover:text-ink"
          >
            Populate from set
          </button>
          <button
            type="button"
            onClick={onOpenTableView}
            className="rounded-lg border border-border px-2.5 py-1.5 text-[11px] font-semibold text-ink-soft hover:text-ink"
          >
            Table view
          </button>
        </div>
      </div>

      <div className="mt-3 flex flex-wrap items-center gap-3 border-t border-border pt-3">
        <div className="flex items-center gap-1">
          {(['all', 'owned', 'missing'] as const).map((option) => (
            <button
              key={option}
              type="button"
              onClick={() => onOwnedFilterChange(option)}
              aria-pressed={ownedFilter === option}
              className={`rounded-lg border px-2 py-1 text-[10.5px] font-semibold capitalize ${
                ownedFilter === option ? 'border-accent text-accent' : 'border-border text-ink-soft'
              }`}
            >
              {option === 'all' ? 'Show all' : option}
            </button>
          ))}
        </div>

        {overlaysEnabled && overlayTags.length > 0 && (
          <div className="flex flex-wrap items-center gap-1.5">
            {overlayTags.map((tag) => {
              const visible = !tagsAreFiltered || visibleTagIds!.has(tag.id)
              return (
                <button
                  key={tag.id}
                  type="button"
                  onClick={() => onToggleTagVisibility(tag.id)}
                  aria-pressed={visible}
                  title={visible ? `Showing ${tag.name} — click to hide` : `Hidden — click to show ${tag.name}`}
                  className={`flex items-center gap-1.5 rounded-lg border px-2 py-1 text-[10.5px] font-semibold transition-opacity ${
                    visible ? 'border-border text-ink-soft' : 'border-border text-ink-faint opacity-40'
                  }`}
                >
                  <span className="h-2.5 w-2.5 rounded-sm" style={{ background: tag.colourHex }} />
                  {tag.name}
                </button>
              )
            })}
            {tagsAreFiltered && (
              <button type="button" onClick={onResetTagVisibility} className="text-[10.5px] font-semibold text-accent">
                Show all tags
              </button>
            )}
          </div>
        )}
      </div>
    </div>
  )
}
