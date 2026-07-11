import { useState, type FormEvent } from 'react'
import type { BinderSummary, UpdateBinderInput } from '../lib/binder-types'
import { ColourPicker } from './ColourPicker'

export function EditBinderForm({
  binder,
  onSubmit,
  onCancel,
  isSubmitting = false,
}: {
  binder: BinderSummary
  onSubmit: (input: UpdateBinderInput) => void
  onCancel: () => void
  isSubmitting?: boolean
}) {
  const [name, setName] = useState(binder.name)
  const [colourHex, setColourHex] = useState(binder.colourHex)
  const [error, setError] = useState<string | null>(null)

  function handleSubmit(e: FormEvent) {
    e.preventDefault()
    if (!name.trim()) {
      setError('Name is required.')
      return
    }
    onSubmit({ name: name.trim(), colourHex })
  }

  return (
    <form onSubmit={handleSubmit} noValidate className="space-y-5">
      <div>
        <label htmlFor="edit-binder-name" className="mb-1.5 block text-xs font-semibold text-ink-soft">
          Name
        </label>
        <input
          id="edit-binder-name"
          type="text"
          value={name}
          onChange={(e) => setName(e.target.value)}
          className="w-full rounded-lg border border-border bg-canvas px-3 py-2 text-sm text-ink focus:border-accent focus:outline-none"
        />
        {error && (
          <p role="alert" className="mt-1 text-xs text-bad">
            {error}
          </p>
        )}
      </div>

      <div>
        <span className="mb-1.5 block text-xs font-semibold text-ink-soft">Colour</span>
        <ColourPicker value={colourHex} onChange={setColourHex} />
      </div>

      <div className="flex justify-end gap-2 pt-1">
        <button
          type="button"
          onClick={onCancel}
          className="rounded-lg border border-border px-4 py-2 text-sm font-semibold text-ink-soft hover:text-ink"
        >
          Cancel
        </button>
        <button
          type="submit"
          disabled={isSubmitting}
          className="rounded-lg bg-accent px-4 py-2 text-sm font-semibold text-accent-ink hover:opacity-90 disabled:opacity-50"
        >
          {isSubmitting ? 'Saving…' : 'Save changes'}
        </button>
      </div>
    </form>
  )
}
