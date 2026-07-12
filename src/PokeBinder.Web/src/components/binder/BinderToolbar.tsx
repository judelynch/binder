import type { OverlayTag } from '../../lib/spread-types'

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
  overlayTags,
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
  overlayTags: OverlayTag[]
}) {
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
        </div>
      </div>

      {overlaysEnabled && overlayTags.length > 0 && (
        <div className="mt-3 flex flex-wrap items-center gap-3 border-t border-border pt-3">
          {overlayTags.map((tag) => (
            <div key={tag.id} className="flex items-center gap-1.5 text-[11px] text-ink-soft">
              <span className="h-2.5 w-2.5 rounded-sm" style={{ background: tag.colourHex }} />
              {tag.name}
            </div>
          ))}
        </div>
      )}
    </div>
  )
}
