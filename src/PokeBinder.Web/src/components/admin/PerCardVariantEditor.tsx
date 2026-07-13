import { useState } from 'react'
import { useCard } from '../../lib/queries/cards'
import { useCardSearch } from '../../lib/queries/search'
import { useAddCardVariant, useRemoveCardVariant, useVariantTypes } from '../../lib/queries/admin'
import { EMPTY_FILTERS } from '../../lib/search-types'
import { useDebouncedValue } from '../../lib/useDebouncedValue'

export function PerCardVariantEditor() {
  const [query, setQuery] = useState('')
  const debouncedQuery = useDebouncedValue(query, 250)
  const [selectedCardId, setSelectedCardId] = useState<string | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)

  const { data: searchResults } = useCardSearch({ ...EMPTY_FILTERS, name: debouncedQuery }, 1, 8)
  const { data: card } = useCard(selectedCardId)
  const { data: variantTypes } = useVariantTypes()
  const addVariant = useAddCardVariant()
  const removeVariant = useRemoveCardVariant()

  function toggle(variantTypeId: string, variantTypeName: string, has: boolean) {
    if (!selectedCardId) return
    setActionError(null)
    const mutation = has ? removeVariant : addVariant
    mutation.mutate(
      { cardId: selectedCardId, variantTypeId },
      {
        onError: (err: unknown) => {
          const message =
            (err as { response?: { data?: { message?: string } } })?.response?.data?.message ??
            `Could not ${has ? 'remove' : 'add'} ${variantTypeName}.`
          setActionError(message)
        },
      },
    )
  }

  return (
    <section className="rounded-2xl border border-border bg-surface p-5">
      <h2 className="font-display text-lg italic text-ink">Per-card variants</h2>
      <input
        type="text"
        value={query}
        onChange={(e) => setQuery(e.target.value)}
        placeholder="Search for a card by name…"
        className="mt-3 w-full rounded-lg border border-border bg-canvas px-2.5 py-1.5 text-sm text-ink placeholder:text-ink-faint"
      />

      {query && !selectedCardId && (
        <div className="mt-2 max-h-48 overflow-y-auto rounded-lg border border-border">
          {searchResults?.items.map((c) => (
            <button
              key={c.id}
              type="button"
              onClick={() => {
                setSelectedCardId(c.id)
                setQuery('')
              }}
              className="block w-full border-b border-border/60 px-3 py-2 text-left text-sm text-ink hover:bg-canvas"
            >
              {c.name} <span className="text-xs text-ink-faint">— {c.setName} #{c.number}</span>
            </button>
          ))}
          {searchResults?.items.length === 0 && <div className="px-3 py-2 text-xs text-ink-faint">No matches.</div>}
        </div>
      )}

      {card && (
        <div className="mt-4 rounded-lg border border-border p-3">
          <div className="flex items-center justify-between">
            <div className="text-sm font-semibold text-ink">{card.name}</div>
            <button type="button" onClick={() => setSelectedCardId(null)} className="text-xs text-ink-soft hover:text-ink">
              Change card
            </button>
          </div>
          <div className="mt-2 space-y-1.5">
            {variantTypes?.map((vt) => {
              const has = card.variantTypeNames.includes(vt.name)
              return (
                <label key={vt.id} className="flex items-center gap-2 text-sm text-ink-soft">
                  <input type="checkbox" checked={has} onChange={() => toggle(vt.id, vt.name, has)} />
                  {vt.name}
                </label>
              )
            })}
          </div>
          {actionError && <p className="mt-2 text-xs text-bad">{actionError}</p>}
        </div>
      )}
    </section>
  )
}
