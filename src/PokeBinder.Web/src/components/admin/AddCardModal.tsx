import { useState } from 'react'
import { Modal } from '../Modal'
import { useSets } from '../../lib/queries/cards'
import { useCreateCard } from '../../lib/queries/admin'
import { SUPERTYPES } from '../../lib/card-options'

export function AddCardModal({ onClose }: { onClose: () => void }) {
  const { data: sets } = useSets()
  const [setId, setSetId] = useState('')
  const createCard = useCreateCard(setId)

  const [id, setId2] = useState('')
  const [number, setNumber] = useState('')
  const [name, setName] = useState('')
  const [supertype, setSupertype] = useState<string>(SUPERTYPES[0])
  const [rarity, setRarity] = useState('')
  const [imageSmallUrl, setImageSmallUrl] = useState('')
  const [imageLargeUrl, setImageLargeUrl] = useState('')

  const canSubmit = setId && id.trim() && number.trim() && name.trim()

  function handleSubmit() {
    createCard.mutate(
      {
        id: id.trim(),
        number: number.trim(),
        name: name.trim(),
        supertype,
        rarity: rarity.trim() || undefined,
        imageSmallUrl: imageSmallUrl.trim() || undefined,
        imageLargeUrl: imageLargeUrl.trim() || undefined,
      },
      { onSuccess: onClose },
    )
  }

  return (
    <Modal title="Add a card manually" onClose={onClose}>
      <p className="mb-3 text-xs text-ink-soft">
        For cards the pokemon-tcg-data repo doesn't have yet. Gets a Normal variant automatically and is marked manual
        origin.
      </p>
      <div className="space-y-3">
        <label className="block text-xs font-semibold text-ink-soft">
          Set
          <select
            value={setId}
            onChange={(e) => setSetId(e.target.value)}
            className="mt-1 w-full rounded-lg border border-border bg-canvas px-2.5 py-1.5 text-sm text-ink"
          >
            <option value="">Select a set…</option>
            {sets?.map((s) => (
              <option key={s.id} value={s.id}>
                {s.name}
              </option>
            ))}
          </select>
        </label>
        <label className="block text-xs font-semibold text-ink-soft">
          Card id (unique, e.g. "sv10-105")
          <input
            type="text"
            value={id}
            onChange={(e) => setId2(e.target.value)}
            className="mt-1 w-full rounded-lg border border-border bg-canvas px-2.5 py-1.5 text-sm text-ink"
          />
        </label>
        <label className="block text-xs font-semibold text-ink-soft">
          Number
          <input
            type="text"
            value={number}
            onChange={(e) => setNumber(e.target.value)}
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
          Supertype
          <select
            value={supertype}
            onChange={(e) => setSupertype(e.target.value)}
            className="mt-1 w-full rounded-lg border border-border bg-canvas px-2.5 py-1.5 text-sm text-ink"
          >
            {SUPERTYPES.map((s) => (
              <option key={s} value={s}>
                {s}
              </option>
            ))}
          </select>
        </label>
        <label className="block text-xs font-semibold text-ink-soft">
          Rarity
          <input
            type="text"
            value={rarity}
            onChange={(e) => setRarity(e.target.value)}
            className="mt-1 w-full rounded-lg border border-border bg-canvas px-2.5 py-1.5 text-sm text-ink"
          />
        </label>
        <label className="block text-xs font-semibold text-ink-soft">
          Small image URL
          <input
            type="text"
            value={imageSmallUrl}
            onChange={(e) => setImageSmallUrl(e.target.value)}
            className="mt-1 w-full rounded-lg border border-border bg-canvas px-2.5 py-1.5 text-sm text-ink"
          />
        </label>
        <label className="block text-xs font-semibold text-ink-soft">
          Large image URL
          <input
            type="text"
            value={imageLargeUrl}
            onChange={(e) => setImageLargeUrl(e.target.value)}
            className="mt-1 w-full rounded-lg border border-border bg-canvas px-2.5 py-1.5 text-sm text-ink"
          />
        </label>

        {createCard.isError && <p className="text-xs text-bad">Could not create card — id may already be in use.</p>}

        <button
          type="button"
          disabled={!canSubmit || createCard.isPending}
          onClick={handleSubmit}
          className="w-full rounded-lg bg-accent py-2 text-sm font-semibold text-accent-ink disabled:opacity-50"
        >
          {createCard.isPending ? 'Creating…' : 'Create card'}
        </button>
      </div>
    </Modal>
  )
}
