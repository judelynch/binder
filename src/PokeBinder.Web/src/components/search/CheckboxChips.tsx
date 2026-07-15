/** Multi-select chip list — like RadioChips, but any number of options can be active at once. */
export function CheckboxChips({
  options,
  selected,
  onChange,
}: {
  options: readonly string[]
  selected: string[]
  onChange: (next: string[]) => void
}) {
  function toggle(value: string) {
    onChange(selected.includes(value) ? selected.filter((v) => v !== value) : [...selected, value])
  }

  return (
    <div className="flex flex-wrap gap-1.5">
      {options.map((option) => {
        const isSelected = selected.includes(option)
        return (
          <button
            type="button"
            key={option}
            aria-pressed={isSelected}
            onClick={() => toggle(option)}
            className={`rounded-full border px-2.5 py-1 text-xs font-semibold transition-colors ${
              isSelected ? 'border-accent bg-accent/15 text-accent' : 'border-border text-ink-soft hover:border-ink-faint hover:text-ink'
            }`}
          >
            {option}
          </button>
        )
      })}
    </div>
  )
}
