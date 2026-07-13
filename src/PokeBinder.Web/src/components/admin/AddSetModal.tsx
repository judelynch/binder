import { useState } from 'react'
import { Modal } from '../Modal'
import { useCreateSet } from '../../lib/queries/admin'

export function AddSetModal({ onClose }: { onClose: () => void }) {
  const createSet = useCreateSet()
  const [id, setId] = useState('')
  const [name, setName] = useState('')
  const [series, setSeries] = useState('')
  const [printedTotal, setPrintedTotal] = useState(0)
  const [releaseDate, setReleaseDate] = useState('')

  const canSubmit = id.trim() && name.trim() && series.trim() && releaseDate

  function handleSubmit() {
    createSet.mutate(
      { id: id.trim(), name: name.trim(), series: series.trim(), printedTotal, total: printedTotal, releaseDate },
      { onSuccess: onClose },
    )
  }

  return (
    <Modal title="Add a set manually" onClose={onClose}>
      <p className="mb-3 text-xs text-ink-soft">
        For sets the pokemon-tcg-data repo doesn't have yet. Marked as manual origin, so a later sync won't overwrite it
        without your confirmation.
      </p>
      <div className="space-y-3">
        <label className="block text-xs font-semibold text-ink-soft">
          Set id (unique, e.g. "sv10")
          <input
            type="text"
            value={id}
            onChange={(e) => setId(e.target.value)}
            className="mt-1 w-full rounded-lg border border-border bg-canvas px-2.5 py-1.5 text-sm text-ink"
          />
        </label>
        <label className="block text-xs font-semibold text-ink-soft">
          Name
          <input
            type="text"
            value={name}
            onChange={(e) => setName(e.target.value)}
            className="mt-1 w-full rounded-lg border border-border bg-canvas px-2.5 py-1.5 text-sm text-ink"
          />
        </label>
        <label className="block text-xs font-semibold text-ink-soft">
          Series
          <input
            type="text"
            value={series}
            onChange={(e) => setSeries(e.target.value)}
            className="mt-1 w-full rounded-lg border border-border bg-canvas px-2.5 py-1.5 text-sm text-ink"
          />
        </label>
        <label className="block text-xs font-semibold text-ink-soft">
          Card count
          <input
            type="number"
            value={printedTotal}
            onChange={(e) => setPrintedTotal(Number(e.target.value))}
            className="mt-1 w-full rounded-lg border border-border bg-canvas px-2.5 py-1.5 text-sm text-ink [font-variant-numeric:tabular-nums]"
          />
        </label>
        <label className="block text-xs font-semibold text-ink-soft">
          Release date
          <input
            type="date"
            value={releaseDate}
            onChange={(e) => setReleaseDate(e.target.value)}
            className="mt-1 w-full rounded-lg border border-border bg-canvas px-2.5 py-1.5 text-sm text-ink"
          />
        </label>

        {createSet.isError && <p className="text-xs text-bad">Could not create set — id may already be in use.</p>}

        <button
          type="button"
          disabled={!canSubmit || createSet.isPending}
          onClick={handleSubmit}
          className="w-full rounded-lg bg-accent py-2 text-sm font-semibold text-accent-ink disabled:opacity-50"
        >
          {createSet.isPending ? 'Creating…' : 'Create set'}
        </button>
      </div>
    </Modal>
  )
}
