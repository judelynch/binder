import { useState } from 'react'
import {
  useCreateVariantType,
  useDeleteVariantType,
  useUpdateVariantType,
  useVariantTypes,
} from '../../lib/queries/admin'

export function VariantTypeManager() {
  const { data: variantTypes } = useVariantTypes()
  const createVariantType = useCreateVariantType()
  const updateVariantType = useUpdateVariantType()
  const deleteVariantType = useDeleteVariantType()

  const [newName, setNewName] = useState('')
  const [editingId, setEditingId] = useState<string | null>(null)
  const [editingName, setEditingName] = useState('')
  const [deleteError, setDeleteError] = useState<string | null>(null)

  function handleDelete(id: string) {
    setDeleteError(null)
    deleteVariantType.mutate(id, {
      onError: (err: unknown) => {
        const message =
          (err as { response?: { data?: { message?: string } } })?.response?.data?.message ??
          'Could not delete this variant type.'
        setDeleteError(message)
      },
    })
  }

  return (
    <section className="rounded-2xl border border-border bg-surface p-5">
      <h2 className="font-display text-lg italic text-ink">Variant types</h2>
      <div className="mt-3 space-y-1.5">
        {variantTypes?.map((vt) => (
          <div key={vt.id} className="flex items-center justify-between gap-2 rounded-lg border border-border px-3 py-2">
            {editingId === vt.id ? (
              <input
                type="text"
                value={editingName}
                onChange={(e) => setEditingName(e.target.value)}
                className="flex-1 rounded border border-border bg-canvas px-2 py-1 text-sm text-ink"
              />
            ) : (
              <span className="text-sm text-ink">{vt.name}</span>
            )}
            <div className="flex gap-1.5 text-xs">
              {editingId === vt.id ? (
                <>
                  <button
                    type="button"
                    onClick={() => {
                      updateVariantType.mutate({ id: vt.id, name: editingName }, { onSuccess: () => setEditingId(null) })
                    }}
                    className="rounded border border-accent px-2 py-1 text-accent"
                  >
                    Save
                  </button>
                  <button type="button" onClick={() => setEditingId(null)} className="rounded border border-border px-2 py-1 text-ink-soft">
                    Cancel
                  </button>
                </>
              ) : (
                <>
                  <button
                    type="button"
                    onClick={() => {
                      setEditingId(vt.id)
                      setEditingName(vt.name)
                    }}
                    className="rounded border border-border px-2 py-1 text-ink-soft hover:text-ink"
                  >
                    Rename
                  </button>
                  <button
                    type="button"
                    onClick={() => handleDelete(vt.id)}
                    className="rounded border border-border px-2 py-1 text-ink-soft hover:text-bad"
                  >
                    Delete
                  </button>
                </>
              )}
            </div>
          </div>
        ))}
      </div>

      {deleteError && <p className="mt-2 text-xs text-bad">{deleteError}</p>}

      <div className="mt-3 flex gap-2">
        <input
          type="text"
          value={newName}
          onChange={(e) => setNewName(e.target.value)}
          placeholder="New variant type name…"
          className="flex-1 rounded-lg border border-border bg-canvas px-2.5 py-1.5 text-sm text-ink placeholder:text-ink-faint"
        />
        <button
          type="button"
          disabled={!newName.trim() || createVariantType.isPending}
          onClick={() => createVariantType.mutate(newName.trim(), { onSuccess: () => setNewName('') })}
          className="rounded-lg bg-accent px-3 py-1.5 text-sm font-semibold text-accent-ink disabled:opacity-50"
        >
          Add
        </button>
      </div>
    </section>
  )
}
