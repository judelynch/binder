import type { CardSearchFilters } from '../../lib/search-types'
import { describeActiveFilters } from '../../lib/search-types'

export function ActiveFilterChips({
  filters,
  onChange,
}: {
  filters: CardSearchFilters
  onChange: (next: CardSearchFilters) => void
}) {
  const chips = describeActiveFilters(filters)
  if (chips.length === 0) return null

  return (
    <div className="flex flex-wrap gap-1.5">
      {chips.map((chip) => (
        <button
          key={chip.key}
          type="button"
          onClick={() => onChange(chip.clear(filters))}
          className="flex items-center gap-1 rounded-full bg-surface-2 px-2.5 py-1 text-xs text-ink-soft hover:text-ink"
        >
          {chip.label}
          <span aria-hidden>×</span>
        </button>
      ))}
    </div>
  )
}
