import { useMemo, useState } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { api } from '../../lib/api'
import type { BulkAssignResult } from '../../lib/binder-types'
import type { CardSummary } from '../../lib/queries/cards'
import { useFullSetCards, useSets } from '../../lib/queries/cards'
import { useBinders } from '../../lib/queries/binders'
import { groupSetCards } from '../../lib/groupSetCards'

type PreviewResult = BulkAssignResult & { currentPageCount: number }

function CardRow({
  card,
  included,
  onToggle,
  selectedVariants,
  onToggleVariant,
}: {
  card: CardSummary
  included: boolean
  onToggle: () => void
  selectedVariants: string[]
  onToggleVariant: (variantId: string) => void
}) {
  const [expanded, setExpanded] = useState(false)

  return (
    <div className="flex items-center gap-2 py-1 text-xs">
      <input type="checkbox" checked={included} onChange={onToggle} className="h-3.5 w-3.5 accent-[var(--color-accent)]" />
      <span className="w-10 shrink-0 text-ink-faint [font-variant-numeric:tabular-nums]">#{card.number}</span>
      <span className="min-w-0 flex-1 truncate text-ink-soft">{card.name}</span>
      {card.variants.length > 1 && (
        <button type="button" onClick={() => setExpanded((v) => !v)} className="shrink-0 text-[10px] font-semibold text-accent">
          {selectedVariants.length} variant{selectedVariants.length === 1 ? '' : 's'} ▾
        </button>
      )}
      {expanded && (
        <div className="ml-2 flex gap-2">
          {card.variants.map((v) => (
            <label key={v.id} className="flex items-center gap-1 text-[10px] text-ink-faint">
              <input
                type="checkbox"
                checked={selectedVariants.includes(v.id)}
                onChange={() => onToggleVariant(v.id)}
                className="h-3 w-3 accent-[var(--color-accent)]"
              />
              {v.variantTypeName}
            </label>
          ))}
        </div>
      )}
    </div>
  )
}

export function SetChecklistPanel({ defaultBinderId, onClose }: { defaultBinderId?: string; onClose: () => void }) {
  const { data: sets } = useSets()
  const { data: binders } = useBinders()
  const [setId, setSetId] = useState('')
  const { data: setCardsPage } = useFullSetCards(setId || null)
  const [excludedIds, setExcludedIds] = useState<Set<string>>(new Set())
  const [variantSelections, setVariantSelections] = useState<Record<string, string[]>>({})
  const [binderId, setBinderId] = useState(defaultBinderId ?? '')
  const [strategy, setStrategy] = useState<'skip' | 'overwrite'>('skip')
  const [preview, setPreview] = useState<PreviewResult | null>(null)
  const [confirmed, setConfirmed] = useState<BulkAssignResult | null>(null)
  const queryClient = useQueryClient()

  const cards = setCardsPage?.items ?? []
  const groups = useMemo(() => groupSetCards(cards), [cards])

  function variantsFor(card: CardSummary): string[] {
    return variantSelections[card.id] ?? (card.variants[0] ? [card.variants[0].id] : [])
  }

  function toggleCard(cardId: string) {
    setExcludedIds((prev) => {
      const next = new Set(prev)
      if (next.has(cardId)) next.delete(cardId)
      else next.add(cardId)
      return next
    })
  }

  function toggleGroup(groupCards: CardSummary[], include: boolean) {
    setExcludedIds((prev) => {
      const next = new Set(prev)
      for (const card of groupCards) {
        if (include) next.delete(card.id)
        else next.add(card.id)
      }
      return next
    })
  }

  function toggleVariant(card: CardSummary, variantId: string) {
    setVariantSelections((prev) => {
      const current = prev[card.id] ?? variantsFor(card)
      const next = current.includes(variantId) ? current.filter((v) => v !== variantId) : [...current, variantId]
      return { ...prev, [card.id]: next.length > 0 ? next : current }
    })
  }

  // Preserve set (numberSortKey) order for the actual insert — the checklist above
  // is grouped by supertype/rarity purely for display, not for placement order.
  const orderedVariantIds = cards
    .filter((c) => !excludedIds.has(c.id))
    .flatMap((c) => variantsFor(c))

  async function resolveStartSlotId(): Promise<string> {
    const spread = (await api.get(`/binders/${binderId}/spread/0`)).data
    const slotId = spread.rightPanel?.slots?.[0]?.slotId
    if (!slotId) throw new Error('This binder has no pages yet — add pages before populating it.')
    return slotId
  }

  const runBulkAssign = useMutation({
    mutationFn: async (dryRun: boolean) => {
      const startSlotId = await resolveStartSlotId()
      const binder = binders?.find((b) => b.id === binderId)
      const result = (
        await api.post<BulkAssignResult>(`/binders/${binderId}/slots/bulk-assign?dryRun=${dryRun}`, {
          cardVariantIds: orderedVariantIds,
          startSlotId,
          occupiedStrategy: strategy,
        })
      ).data
      return { result, currentPageCount: binder?.pageCount ?? 0 }
    },
    onSuccess: ({ result, currentPageCount }, dryRun) => {
      if (dryRun) {
        setPreview({ ...result, currentPageCount })
      } else {
        setConfirmed(result)
        queryClient.invalidateQueries({ queryKey: ['spread', binderId] })
        queryClient.invalidateQueries({ queryKey: ['binders'] })
        queryClient.invalidateQueries({ queryKey: ['dashboard'] })
      }
    },
  })

  return (
    <div className="fixed inset-0 z-50">
      <button aria-label="Close set builder" className="absolute inset-0 bg-black/60" onClick={onClose} />
      <div className="absolute inset-y-0 right-0 flex w-full flex-col border-l border-border bg-surface shadow-2xl sm:w-[90vw] lg:w-[70vw] xl:w-[55vw]">
        <div className="flex items-center justify-between border-b border-border px-4 py-3">
          <h2 className="font-display text-base font-semibold text-ink">Populate from a set</h2>
          <button
            type="button"
            aria-label="Close"
            onClick={onClose}
            className="flex h-8 w-8 items-center justify-center rounded-lg border border-border text-ink-soft"
          >
            ×
          </button>
        </div>

        {confirmed ? (
          <div className="flex-1 p-6">
            <p className="text-sm text-ink">
              Placed <span className="font-semibold">{confirmed.placed}</span> cards
              {confirmed.pagesAdded > 0 && (
                <>
                  {' '}
                  and added <span className="font-semibold">{confirmed.pagesAdded}</span> pages
                </>
              )}
              .
            </p>
            <button type="button" onClick={onClose} className="mt-4 rounded-lg bg-accent px-4 py-2 text-sm font-semibold text-accent-ink">
              Done
            </button>
          </div>
        ) : (
          <>
            <div className="border-b border-border p-4">
              <label htmlFor="set-picker" className="mb-1.5 block text-xs font-semibold text-ink-soft">
                Set
              </label>
              <select
                id="set-picker"
                value={setId}
                onChange={(e) => {
                  setSetId(e.target.value)
                  setExcludedIds(new Set())
                  setVariantSelections({})
                  setPreview(null)
                }}
                className="w-full rounded-lg border border-border bg-canvas px-3 py-2 text-sm text-ink"
              >
                <option value="">Choose a set…</option>
                {sets?.map((s) => (
                  <option key={s.id} value={s.id}>
                    {s.name} ({s.total})
                  </option>
                ))}
              </select>
            </div>

            {setId && (
              <div className="min-h-0 flex-1 overflow-y-auto px-4">
                {groups.map((group) => (
                  <div key={group.supertype} className="border-b border-border py-2">
                    <div className="flex items-center justify-between">
                      <h3 className="font-display text-sm font-semibold text-ink">{group.supertype}</h3>
                      <div className="flex gap-2 text-[10px] font-semibold text-accent">
                        <button type="button" onClick={() => toggleGroup(group.rarityGroups.flatMap((r) => r.cards), true)}>
                          All
                        </button>
                        <button type="button" onClick={() => toggleGroup(group.rarityGroups.flatMap((r) => r.cards), false)}>
                          None
                        </button>
                      </div>
                    </div>
                    {group.rarityGroups.map((rg) => (
                      <div key={rg.rarity} className="mt-1.5">
                        <div className="flex items-center justify-between">
                          <span className="text-[10.5px] font-semibold uppercase tracking-wide text-ink-faint">{rg.rarity}</span>
                          <div className="flex gap-2 text-[10px] font-semibold text-accent">
                            <button type="button" onClick={() => toggleGroup(rg.cards, true)}>
                              All
                            </button>
                            <button type="button" onClick={() => toggleGroup(rg.cards, false)}>
                              None
                            </button>
                          </div>
                        </div>
                        {rg.cards.map((card) => (
                          <CardRow
                            key={card.id}
                            card={card}
                            included={!excludedIds.has(card.id)}
                            onToggle={() => toggleCard(card.id)}
                            selectedVariants={variantsFor(card)}
                            onToggleVariant={(variantId) => toggleVariant(card, variantId)}
                          />
                        ))}
                      </div>
                    ))}
                  </div>
                ))}
              </div>
            )}

            {setId && (
              <div className="border-t border-border p-4">
                {preview ? (
                  <div className="rounded-lg border border-accent/40 bg-accent/10 p-3 text-xs text-ink">
                    <p>
                      This will fill <span className="font-semibold">{preview.placed}</span> slots
                      {preview.pagesAdded > 0 ? (
                        <>
                          {' '}
                          across new pages — binder currently has{' '}
                          <span className="font-semibold">{preview.currentPageCount}</span> pages, needs{' '}
                          <span className="font-semibold">{preview.pagesAdded}</span> more.
                        </>
                      ) : (
                        '.'
                      )}
                    </p>
                    <div className="mt-2 flex gap-2">
                      <button
                        type="button"
                        onClick={() => setPreview(null)}
                        className="rounded-lg border border-border px-3 py-1.5 text-xs font-semibold text-ink-soft"
                      >
                        Back
                      </button>
                      <button
                        type="button"
                        disabled={runBulkAssign.isPending}
                        onClick={() => runBulkAssign.mutate(false)}
                        className="rounded-lg bg-accent px-3 py-1.5 text-xs font-semibold text-accent-ink disabled:opacity-50"
                      >
                        {runBulkAssign.isPending ? 'Placing…' : preview.pagesAdded > 0 ? `Add pages & confirm` : 'Confirm'}
                      </button>
                    </div>
                  </div>
                ) : (
                  <>
                    <div className="flex flex-wrap items-end gap-3">
                      <div>
                        <label htmlFor="checklist-binder" className="mb-1 block text-[10.5px] font-semibold text-ink-soft">
                          Target binder
                        </label>
                        <select
                          id="checklist-binder"
                          value={binderId}
                          onChange={(e) => setBinderId(e.target.value)}
                          className="rounded-lg border border-border bg-canvas px-2 py-1.5 text-xs text-ink"
                        >
                          <option value="" disabled>
                            Choose…
                          </option>
                          {binders?.map((b) => (
                            <option key={b.id} value={b.id}>
                              {b.name}
                            </option>
                          ))}
                        </select>
                      </div>
                      <div className="flex gap-1.5">
                        <button
                          type="button"
                          onClick={() => setStrategy('skip')}
                          aria-pressed={strategy === 'skip'}
                          className={`rounded-lg border px-2.5 py-1.5 text-[10.5px] font-semibold ${strategy === 'skip' ? 'border-accent text-accent' : 'border-border text-ink-soft'}`}
                        >
                          Skip filled
                        </button>
                        <button
                          type="button"
                          onClick={() => setStrategy('overwrite')}
                          aria-pressed={strategy === 'overwrite'}
                          className={`rounded-lg border px-2.5 py-1.5 text-[10.5px] font-semibold ${strategy === 'overwrite' ? 'border-accent text-accent' : 'border-border text-ink-soft'}`}
                        >
                          Overwrite
                        </button>
                      </div>
                      <span className="text-[10.5px] text-ink-faint">{orderedVariantIds.length} cards selected</span>
                    </div>
                    <button
                      type="button"
                      disabled={!binderId || orderedVariantIds.length === 0 || runBulkAssign.isPending}
                      onClick={() => runBulkAssign.mutate(true)}
                      className="mt-3 w-full rounded-lg bg-accent py-2 text-sm font-semibold text-accent-ink disabled:opacity-50"
                    >
                      {runBulkAssign.isPending ? 'Checking…' : 'Preview'}
                    </button>
                    {runBulkAssign.isError && <p className="mt-2 text-xs text-bad">{(runBulkAssign.error as Error).message}</p>}
                  </>
                )}
              </div>
            )}
          </>
        )}
      </div>
    </div>
  )
}
