export function RadioChips({
  options,
  value,
  onChange,
}: {
  options: readonly string[]
  value: string | null
  onChange: (next: string | null) => void
}) {
  return (
    <div className="flex flex-wrap gap-1.5">
      {options.map((option) => {
        const selected = value === option
        return (
          <button
            type="button"
            key={option}
            aria-pressed={selected}
            onClick={() => onChange(selected ? null : option)}
            className={`rounded-full border px-2.5 py-1 text-xs font-semibold ${
              selected ? 'border-accent text-accent' : 'border-border text-ink-soft'
            }`}
          >
            {option}
          </button>
        )
      })}
    </div>
  )
}
