import { useNavigate } from 'react-router-dom'
import type { BinderSummary } from '../lib/binder-types'
import { CompletenessBar } from './CompletenessBar'

function formatDate(iso: string) {
  return new Date(iso).toLocaleDateString(undefined, { month: 'short', day: 'numeric', year: 'numeric' })
}

export function BinderCard({
  binder,
  onEdit,
  onDelete,
}: {
  binder: BinderSummary
  onEdit: () => void
  onDelete: () => void
}) {
  const navigate = useNavigate()
  const completeness = binder.filledSlots > 0 ? (binder.ownedCount / binder.filledSlots) * 100 : 0

  return (
    <div
      role="button"
      tabIndex={0}
      onClick={() => navigate(`/binders/${binder.id}`)}
      onKeyDown={(e) => {
        if (e.key === 'Enter') navigate(`/binders/${binder.id}`)
      }}
      className="cursor-pointer rounded-xl border border-border p-4 text-left transition-transform hover:-translate-y-0.5 focus:outline-none focus:ring-2 focus:ring-accent"
      style={{
        background: `radial-gradient(circle at 15% 0%, ${binder.colourHex}3d, transparent 60%), var(--color-surface)`,
      }}
    >
      <div className="flex items-start justify-between gap-2">
        <div className="min-w-0">
          <div className="truncate font-display text-base font-semibold text-ink">{binder.name}</div>
          <div className="mt-0.5 text-xs text-ink-soft">
            Created {formatDate(binder.createdAt)} · {binder.rows}×{binder.columns} layout
          </div>
        </div>
        <div className="flex shrink-0 gap-1">
          <button
            type="button"
            aria-label={`Edit ${binder.name}`}
            onClick={(e) => {
              e.stopPropagation()
              onEdit()
            }}
            className="flex h-7 w-7 items-center justify-center rounded-lg border border-border text-ink-soft hover:border-accent hover:text-ink"
          >
            ✎
          </button>
          <button
            type="button"
            aria-label={`Delete ${binder.name}`}
            onClick={(e) => {
              e.stopPropagation()
              onDelete()
            }}
            className="flex h-7 w-7 items-center justify-center rounded-lg border border-border text-ink-soft hover:border-bad hover:text-bad"
          >
            ✕
          </button>
        </div>
      </div>

      <div className="mt-3 text-xs text-ink-soft">
        {binder.pageCount} pages ·{' '}
        <span className="font-medium text-ink">
          {binder.filledSlots}/{binder.totalSlots}
        </span>{' '}
        slots filled
      </div>
      <div className="mt-1.5 flex gap-3 text-xs">
        <span className="text-good">
          <span className="font-medium">{binder.ownedCount}</span> owned
        </span>
        <span className="text-bad">
          <span className="font-medium">{binder.missingCount}</span> missing
        </span>
      </div>
      <div className="mt-2.5">
        <CompletenessBar percent={completeness} />
      </div>
    </div>
  )
}
