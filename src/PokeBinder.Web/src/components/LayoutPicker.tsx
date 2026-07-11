export interface Layout {
  rows: number
  columns: number
  label: string
}

export const LAYOUTS: Layout[] = [
  { rows: 2, columns: 2, label: '2×2' },
  { rows: 3, columns: 3, label: '3×3' },
  { rows: 3, columns: 4, label: '3×4' },
]

export function LayoutPicker({
  value,
  onChange,
}: {
  value: Layout
  onChange: (layout: Layout) => void
}) {
  return (
    <div className="flex gap-3">
      {LAYOUTS.map((layout) => {
        const selected = value.rows === layout.rows && value.columns === layout.columns
        return (
          <button
            type="button"
            key={layout.label}
            onClick={() => onChange(layout)}
            aria-pressed={selected}
            className="flex flex-col items-center gap-1.5"
          >
            <span
              className={`grid h-16 w-16 place-items-center rounded-lg ${
                selected ? 'border-2 border-accent bg-surface' : 'border border-border bg-surface'
              }`}
            >
              <span
                className="grid gap-0.5"
                style={{ gridTemplateColumns: `repeat(${layout.columns}, 1fr)` }}
              >
                {Array.from({ length: layout.rows * layout.columns }).map((_, i) => (
                  <span
                    key={i}
                    className={`h-2 w-2 rounded-[2px] ${selected ? 'bg-accent' : 'bg-ink-faint'}`}
                  />
                ))}
              </span>
            </span>
            <span className={`text-[11px] font-semibold ${selected ? 'text-ink' : 'text-ink-soft'}`}>
              {layout.label}
            </span>
          </button>
        )
      })}
    </div>
  )
}
