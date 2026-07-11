import { BINDER_COLOURS } from '../lib/binder-colours'

export function ColourPicker({ value, onChange }: { value: string; onChange: (hex: string) => void }) {
  return (
    <div className="flex flex-wrap gap-2">
      {BINDER_COLOURS.map((colour) => {
        const selected = colour.hex.toLowerCase() === value.toLowerCase()
        return (
          <button
            type="button"
            key={colour.hex}
            aria-label={colour.name}
            aria-pressed={selected}
            onClick={() => onChange(colour.hex)}
            className={`h-9 w-9 rounded-full transition-transform ${
              selected ? 'ring-2 ring-accent ring-offset-2 ring-offset-surface' : 'hover:scale-105'
            }`}
            style={{ background: colour.hex }}
          />
        )
      })}
    </div>
  )
}
