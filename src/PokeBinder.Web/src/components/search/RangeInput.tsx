export function RangeInput({
  min,
  max,
  onChange,
  unit,
}: {
  min: number | null
  max: number | null
  onChange: (min: number | null, max: number | null) => void
  unit?: string
}) {
  function parse(value: string): number | null {
    if (value === '') return null
    const n = Number(value)
    return Number.isNaN(n) ? null : n
  }

  return (
    <div className="flex items-center gap-2">
      <input
        type="number"
        inputMode="numeric"
        placeholder="Min"
        value={min ?? ''}
        onChange={(e) => onChange(parse(e.target.value), max)}
        className="w-20 rounded-lg border border-border bg-canvas px-2 py-1.5 text-xs text-ink [font-variant-numeric:tabular-nums] focus:border-accent focus:outline-none"
      />
      <span className="text-ink-faint">–</span>
      <input
        type="number"
        inputMode="numeric"
        placeholder="Max"
        value={max ?? ''}
        onChange={(e) => onChange(min, parse(e.target.value))}
        className="w-20 rounded-lg border border-border bg-canvas px-2 py-1.5 text-xs text-ink [font-variant-numeric:tabular-nums] focus:border-accent focus:outline-none"
      />
      {unit && <span className="text-xs text-ink-faint">{unit}</span>}
    </div>
  )
}
