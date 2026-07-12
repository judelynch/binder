export function CheckboxList({
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
    <div className="grid max-h-52 grid-cols-2 gap-x-3 gap-y-1.5 overflow-y-auto pr-1">
      {options.map((option) => (
        <label key={option} className="flex items-center gap-2 text-xs text-ink-soft">
          <input
            type="checkbox"
            checked={selected.includes(option)}
            onChange={() => toggle(option)}
            className="h-3.5 w-3.5 accent-[var(--color-accent)]"
          />
          <span className="truncate">{option}</span>
        </label>
      ))}
    </div>
  )
}
