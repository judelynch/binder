import { Link } from 'react-router-dom'
import type { DashboardBinder } from '../lib/binder-types'

export function RecentBinderTile({ binder }: { binder: DashboardBinder }) {
  return (
    <Link
      to={`/binders/${binder.id}`}
      className="block w-36 shrink-0 rounded-xl border border-border p-3 transition-transform hover:-translate-y-0.5"
      style={{
        background: `radial-gradient(circle at 30% 20%, ${binder.colourHex}55, transparent 70%), var(--color-surface)`,
      }}
    >
      <div className="truncate text-sm font-semibold text-ink">{binder.name}</div>
      <div className="mt-1 text-[11px] text-ink-soft">{Math.round(binder.completenessPercent)}% complete</div>
    </Link>
  )
}
