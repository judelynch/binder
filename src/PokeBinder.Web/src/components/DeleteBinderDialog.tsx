import { useState } from 'react'
import type { BinderSummary } from '../lib/binder-types'
import { Modal } from './Modal'

export function DeleteBinderDialog({
  binder,
  onConfirm,
  onCancel,
  isDeleting = false,
}: {
  binder: BinderSummary
  onConfirm: () => void
  onCancel: () => void
  isDeleting?: boolean
}) {
  const [typedName, setTypedName] = useState('')
  const matches = typedName === binder.name

  return (
    <Modal title={`Delete "${binder.name}"?`} onClose={onCancel}>
      <p className="text-sm text-ink-soft">
        This permanently deletes the binder, its pages, and every slot assignment inside it. This can't be undone.
      </p>
      <p className="mt-3 text-sm text-ink-soft">
        Type <span className="font-semibold text-ink">{binder.name}</span> to confirm.
      </p>
      <input
        aria-label="Confirm binder name"
        type="text"
        value={typedName}
        onChange={(e) => setTypedName(e.target.value)}
        className="mt-2 w-full rounded-lg border border-border bg-canvas px-3 py-2 text-sm text-ink focus:border-bad focus:outline-none"
      />
      <div className="mt-5 flex justify-end gap-2">
        <button
          type="button"
          onClick={onCancel}
          className="rounded-lg border border-border px-4 py-2 text-sm font-semibold text-ink-soft hover:text-ink"
        >
          Cancel
        </button>
        <button
          type="button"
          disabled={!matches || isDeleting}
          onClick={onConfirm}
          className="rounded-lg bg-bad px-4 py-2 text-sm font-semibold text-white disabled:opacity-40"
        >
          {isDeleting ? 'Deleting…' : 'Delete binder'}
        </button>
      </div>
    </Modal>
  )
}
