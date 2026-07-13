import { useState } from 'react'
import { Modal } from '../Modal'
import { useCard } from '../../lib/queries/cards'
import { useCardAudit, useUpdateCard } from '../../lib/queries/admin'

export function EditCardModal({ cardId, onClose }: { cardId: string; onClose: () => void }) {
  const { data: card, isPending } = useCard(cardId)
  const { data: audit } = useCardAudit(cardId)
  const updateCard = useUpdateCard(cardId)

  const [name, setName] = useState<string | null>(null)
  const [rarity, setRarity] = useState<string | null>(null)
  const [artist, setArtist] = useState<string | null>(null)
  const [flavorText, setFlavorText] = useState<string | null>(null)
  const [regulationMark, setRegulationMark] = useState<string | null>(null)
  const [imageSmallUrl, setImageSmallUrl] = useState<string | null>(null)
  const [imageLargeUrl, setImageLargeUrl] = useState<string | null>(null)
  const [auditNote, setAuditNote] = useState('')

  if (isPending || !card) {
    return (
      <Modal title="Edit card" onClose={onClose}>
        <p className="text-sm text-ink-soft">Loading…</p>
      </Modal>
    )
  }

  function handleSave() {
    updateCard.mutate(
      {
        name: name ?? undefined,
        rarity: rarity ?? undefined,
        artist: artist ?? undefined,
        flavorText: flavorText ?? undefined,
        regulationMark: regulationMark ?? undefined,
        imageSmallUrl: imageSmallUrl ?? undefined,
        imageLargeUrl: imageLargeUrl ?? undefined,
        auditNote,
      },
      { onSuccess: onClose },
    )
  }

  return (
    <Modal title={`Edit ${card.name}`} onClose={onClose}>
      <div className="space-y-3">
        <label className="block text-xs font-semibold text-ink-soft">
          Name
          <input
            type="text"
            defaultValue={card.name}
            onChange={(e) => setName(e.target.value)}
            className="mt-1 w-full rounded-lg border border-border bg-canvas px-2.5 py-1.5 text-sm text-ink"
          />
        </label>
        <label className="block text-xs font-semibold text-ink-soft">
          Rarity
          <input
            type="text"
            defaultValue={card.rarity ?? ''}
            onChange={(e) => setRarity(e.target.value)}
            className="mt-1 w-full rounded-lg border border-border bg-canvas px-2.5 py-1.5 text-sm text-ink"
          />
        </label>
        <label className="block text-xs font-semibold text-ink-soft">
          Artist
          <input
            type="text"
            defaultValue={card.artist ?? ''}
            onChange={(e) => setArtist(e.target.value)}
            className="mt-1 w-full rounded-lg border border-border bg-canvas px-2.5 py-1.5 text-sm text-ink"
          />
        </label>
        <label className="block text-xs font-semibold text-ink-soft">
          Regulation mark
          <input
            type="text"
            defaultValue={card.regulationMark ?? ''}
            onChange={(e) => setRegulationMark(e.target.value)}
            className="mt-1 w-full rounded-lg border border-border bg-canvas px-2.5 py-1.5 text-sm text-ink"
          />
        </label>
        <label className="block text-xs font-semibold text-ink-soft">
          Small image URL
          <input
            type="text"
            defaultValue={card.imageSmallUrl ?? ''}
            onChange={(e) => setImageSmallUrl(e.target.value)}
            className="mt-1 w-full rounded-lg border border-border bg-canvas px-2.5 py-1.5 text-sm text-ink"
          />
        </label>
        <label className="block text-xs font-semibold text-ink-soft">
          Large image URL
          <input
            type="text"
            defaultValue={card.imageLargeUrl ?? ''}
            onChange={(e) => setImageLargeUrl(e.target.value)}
            className="mt-1 w-full rounded-lg border border-border bg-canvas px-2.5 py-1.5 text-sm text-ink"
          />
        </label>
        <label className="block text-xs font-semibold text-ink-soft">
          Flavor text
          <textarea
            defaultValue={card.flavorText ?? ''}
            onChange={(e) => setFlavorText(e.target.value)}
            rows={2}
            className="mt-1 w-full rounded-lg border border-border bg-canvas px-2.5 py-1.5 text-sm text-ink"
          />
        </label>
        <label className="block text-xs font-semibold text-ink-soft">
          Audit note <span className="text-bad">*</span>
          <input
            type="text"
            value={auditNote}
            onChange={(e) => setAuditNote(e.target.value)}
            placeholder="Why is this being corrected?"
            className="mt-1 w-full rounded-lg border border-border bg-canvas px-2.5 py-1.5 text-sm text-ink placeholder:text-ink-faint"
          />
        </label>

        {updateCard.isError && <p className="text-xs text-bad">Could not save. Check the audit note and try again.</p>}

        <button
          type="button"
          disabled={!auditNote.trim() || updateCard.isPending}
          onClick={handleSave}
          className="w-full rounded-lg bg-accent py-2 text-sm font-semibold text-accent-ink disabled:opacity-50"
        >
          {updateCard.isPending ? 'Saving…' : 'Save changes'}
        </button>

        {audit && audit.length > 0 && (
          <div className="border-t border-border pt-3">
            <div className="text-xs font-semibold uppercase tracking-wide text-ink-faint">Edit history</div>
            <ul className="mt-2 space-y-2">
              {audit.map((entry) => (
                <li key={entry.id} className="text-xs text-ink-soft">
                  <span className="text-ink">{entry.editedByEmail}</span> · {new Date(entry.editedAt).toLocaleString()}
                  <div className="text-ink-faint">
                    {entry.changedFields.join(', ')} — "{entry.note}"
                  </div>
                </li>
              ))}
            </ul>
          </div>
        )}
      </div>
    </Modal>
  )
}
