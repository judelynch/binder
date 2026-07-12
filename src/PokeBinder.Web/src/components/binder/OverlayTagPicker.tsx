import { useState } from 'react'
import { BINDER_COLOURS } from '../../lib/binder-colours'
import { useCreateOverlayTag } from '../../lib/queries/overlay-tags'
import type { OverlayTag } from '../../lib/spread-types'

export function OverlayTagPicker({
  tags,
  selectedId,
  onSelect,
}: {
  tags: OverlayTag[]
  selectedId: string | null
  onSelect: (id: string | null) => void
}) {
  const [creating, setCreating] = useState(false)
  const [name, setName] = useState('')
  const [colourHex, setColourHex] = useState(BINDER_COLOURS[0].hex)
  const createTag = useCreateOverlayTag()

  function handleCreate() {
    if (!name.trim()) return
    createTag.mutate(
      { name: name.trim(), colourHex },
      {
        onSuccess: (tag) => {
          onSelect(tag.id)
          setCreating(false)
          setName('')
        },
      },
    )
  }

  return (
    <div className="flex flex-wrap items-center gap-2">
      <button
        type="button"
        onClick={() => onSelect(null)}
        aria-pressed={selectedId === null}
        className={`rounded-full border px-2.5 py-1 text-xs font-semibold ${
          selectedId === null ? 'border-accent text-accent' : 'border-border text-ink-soft'
        }`}
      >
        None
      </button>
      {tags.map((tag) => (
        <button
          type="button"
          key={tag.id}
          onClick={() => onSelect(tag.id)}
          aria-pressed={selectedId === tag.id}
          className={`flex items-center gap-1.5 rounded-full border px-2.5 py-1 text-xs font-semibold ${
            selectedId === tag.id ? 'border-accent text-ink' : 'border-border text-ink-soft'
          }`}
        >
          <span className="h-2 w-2 rounded-full" style={{ background: tag.colourHex }} />
          {tag.name}
        </button>
      ))}

      {creating ? (
        <div className="mt-2 flex w-full items-center gap-2">
          <input
            autoFocus
            type="text"
            value={name}
            onChange={(e) => setName(e.target.value)}
            placeholder="Tag name"
            className="min-w-0 flex-1 rounded-lg border border-border bg-canvas px-2.5 py-1.5 text-xs text-ink focus:border-accent focus:outline-none"
          />
          <div className="flex gap-1">
            {BINDER_COLOURS.slice(0, 5).map((c) => (
              <button
                key={c.hex}
                type="button"
                aria-label={c.name}
                onClick={() => setColourHex(c.hex)}
                className={`h-6 w-6 rounded-full ${colourHex === c.hex ? 'ring-2 ring-accent ring-offset-1 ring-offset-surface' : ''}`}
                style={{ background: c.hex }}
              />
            ))}
          </div>
          <button
            type="button"
            onClick={handleCreate}
            disabled={createTag.isPending}
            className="rounded-lg bg-accent px-2.5 py-1.5 text-xs font-semibold text-accent-ink"
          >
            Add
          </button>
        </div>
      ) : (
        <button
          type="button"
          onClick={() => setCreating(true)}
          className="rounded-full border border-dashed border-border px-2.5 py-1 text-xs font-semibold text-ink-faint"
        >
          + New tag
        </button>
      )}
    </div>
  )
}
