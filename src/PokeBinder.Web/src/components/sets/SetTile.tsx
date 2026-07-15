import { Link } from 'react-router-dom'
import type { SetSummary } from '../../lib/queries/cards'
import { SetLogo } from './SetLogo'

export function SetTile({ set }: { set: SetSummary }) {
  const percent = set.cardCount === 0 ? 0 : Math.round((set.ownedCount / set.cardCount) * 100)

  return (
    <Link
      to={`/sets/${set.id}`}
      className="group flex flex-col overflow-hidden rounded-xl border border-border bg-surface transition-colors hover:border-accent"
    >
      <div className="flex h-20 items-center justify-center border-b border-border bg-surface-2 p-3">
        <SetLogo src={set.logoImageUrl} alt={set.name} />
      </div>

      <div className="flex flex-1 flex-col justify-between p-4">
        <div>
          <div className="truncate font-display text-sm font-semibold text-ink group-hover:text-accent">{set.name}</div>
          <div className="truncate text-xs text-ink-faint">{set.series}</div>
        </div>
        <div className="mt-3 flex items-center justify-between text-xs text-ink-soft">
          <span className="[font-variant-numeric:tabular-nums]">{set.cardCount} cards</span>
          <span className="[font-variant-numeric:tabular-nums]">{percent}%</span>
        </div>
        <div className="mt-2 h-1.5 overflow-hidden rounded-full bg-surface-2">
          <div className="h-full rounded-full bg-accent transition-[width]" style={{ width: `${percent}%` }} />
        </div>
      </div>
    </Link>
  )
}
