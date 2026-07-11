import { useState, type FormEvent } from 'react'
import { BINDER_COLOURS } from '../lib/binder-colours'
import type { CreateBinderInput } from '../lib/binder-types'
import { ColourPicker } from './ColourPicker'
import { LAYOUTS, LayoutPicker, type Layout } from './LayoutPicker'

export const MIN_PAGES = 0
export const MAX_PAGES = 60

interface FormErrors {
  name?: string
  pageCount?: string
}

export function CreateBinderForm({
  onSubmit,
  onCancel,
  isSubmitting = false,
}: {
  onSubmit: (input: CreateBinderInput) => void
  onCancel: () => void
  isSubmitting?: boolean
}) {
  const [name, setName] = useState('')
  const [colourHex, setColourHex] = useState(BINDER_COLOURS[0].hex)
  const [layout, setLayout] = useState<Layout>(LAYOUTS[1])
  const [pageCount, setPageCount] = useState(4)
  const [errors, setErrors] = useState<FormErrors>({})

  function validate(): boolean {
    const next: FormErrors = {}
    if (!name.trim()) {
      next.name = 'Name is required.'
    }
    if (pageCount < MIN_PAGES || pageCount > MAX_PAGES) {
      next.pageCount = `Page count must be between ${MIN_PAGES} and ${MAX_PAGES}.`
    } else if (pageCount % 2 !== 0) {
      next.pageCount = 'Page count must be even — pages come in sheet pairs.'
    }
    setErrors(next)
    return Object.keys(next).length === 0
  }

  function handleSubmit(e: FormEvent) {
    e.preventDefault()
    if (!validate()) return
    onSubmit({ name: name.trim(), colourHex, rows: layout.rows, columns: layout.columns, initialPageCount: pageCount })
  }

  function adjustPageCount(delta: number) {
    setPageCount((current) => Math.max(MIN_PAGES, Math.min(MAX_PAGES, current + delta)))
  }

  return (
    <form onSubmit={handleSubmit} noValidate className="space-y-5">
      <div>
        <label htmlFor="binder-name" className="mb-1.5 block text-xs font-semibold text-ink-soft">
          Name
        </label>
        <input
          id="binder-name"
          type="text"
          value={name}
          onChange={(e) => setName(e.target.value)}
          placeholder="e.g. Base Set Masters"
          className="w-full rounded-lg border border-border bg-canvas px-3 py-2 text-sm text-ink placeholder:text-ink-faint focus:border-accent focus:outline-none"
        />
        {errors.name && (
          <p role="alert" className="mt-1 text-xs text-bad">
            {errors.name}
          </p>
        )}
      </div>

      <div>
        <span className="mb-1.5 block text-xs font-semibold text-ink-soft">Colour</span>
        <ColourPicker value={colourHex} onChange={setColourHex} />
      </div>

      <div>
        <span className="mb-1.5 block text-xs font-semibold text-ink-soft">Layout</span>
        <LayoutPicker value={layout} onChange={setLayout} />
      </div>

      <div>
        <label htmlFor="binder-page-count" className="mb-1.5 block text-xs font-semibold text-ink-soft">
          Starting page count
        </label>
        <div className="flex items-center gap-2">
          <button
            type="button"
            aria-label="Decrease page count"
            onClick={() => adjustPageCount(-2)}
            className="flex h-9 w-9 items-center justify-center rounded-lg border border-border text-ink-soft hover:border-accent hover:text-ink"
          >
            −
          </button>
          <input
            id="binder-page-count"
            type="number"
            inputMode="numeric"
            value={pageCount}
            onChange={(e) => setPageCount(Number(e.target.value))}
            className="w-20 rounded-lg border border-border bg-canvas px-3 py-2 text-center text-sm text-ink [font-variant-numeric:tabular-nums] focus:border-accent focus:outline-none"
          />
          <button
            type="button"
            aria-label="Increase page count"
            onClick={() => adjustPageCount(2)}
            className="flex h-9 w-9 items-center justify-center rounded-lg border border-border text-ink-soft hover:border-accent hover:text-ink"
          >
            +
          </button>
          <span className="text-xs text-ink-faint">{pageCount * layout.rows * layout.columns} slots total</span>
        </div>
        {errors.pageCount && (
          <p role="alert" className="mt-1 text-xs text-bad">
            {errors.pageCount}
          </p>
        )}
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
          {isSubmitting ? 'Creating…' : 'Create binder'}
        </button>
      </div>
    </form>
  )
}
