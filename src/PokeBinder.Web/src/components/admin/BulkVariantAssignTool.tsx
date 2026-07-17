import { useState } from 'react'
import { SearchFilterPanel } from '../search/SearchFilterPanel'
import { ResultsGrid } from '../search/ResultsGrid'
import { useBulkAssignVariants, useVariantTypes } from '../../lib/queries/admin'
import { useCardSearch } from '../../lib/queries/search'
import { EMPTY_FILTERS } from '../../lib/search-types'
import type { BulkVariantAssignResult } from '../../lib/admin-types'

const NO_SELECTION = new Set<string>()

export function BulkVariantAssignTool() {
  const [filters, setFilters] = useState(EMPTY_FILTERS)
  const [selectedVariantTypeIds, setSelectedVariantTypeIds] = useState<Set<string>>(new Set())
  const [preview, setPreview] = useState<BulkVariantAssignResult | null>(null)
  const [applied, setApplied] = useState<BulkVariantAssignResult | null>(null)

  const { data: variantTypes } = useVariantTypes()
  const { data: matches, isPending: matchesPending } = useCardSearch(filters, 1, 60)
  const bulkAssign = useBulkAssignVariants()

  function toggleVariantType(id: string) {
    setPreview(null)
    setApplied(null)
    setSelectedVariantTypeIds((prev) => {
      const next = new Set(prev)
      if (next.has(id)) next.delete(id)
      else next.add(id)
      return next
    })
  }

  function handlePreview() {
    setApplied(null)
    bulkAssign.mutate(
      { filters, variantTypeIds: Array.from(selectedVariantTypeIds), dryRun: true },
      { onSuccess: setPreview },
    )
  }

  function handleApply() {
    bulkAssign.mutate(
      { filters, variantTypeIds: Array.from(selectedVariantTypeIds), dryRun: false },
      { onSuccess: setApplied },
    )
  }

  const canRun = selectedVariantTypeIds.size > 0

  return (
    <section className="rounded-2xl border border-border bg-surface p-5">
      <h2 className="font-display text-lg italic text-ink">Bulk variant assignment</h2>
      <p className="mt-1 text-xs text-ink-soft">
        Filter to a card population, pick one or more variant types, and create the missing variant rows across every
        matching card in one go — e.g. add Reverse Holo to all Commons/Uncommons in a set.
      </p>

      <div className="mt-4 grid grid-cols-1 gap-4 lg:grid-cols-[280px_1fr]">
        <div className="rounded-xl border border-border lg:h-[420px]">
          <div className="h-full max-h-[420px] overflow-y-auto">
            <SearchFilterPanel
              filters={filters}
              onChange={(next) => {
                setFilters(next)
                setPreview(null)
                setApplied(null)
              }}
              resultCount={matches?.totalCount ?? null}
            />
          </div>
        </div>

        <div className="flex flex-col gap-3">
          <div className="rounded-xl border border-border p-3">
            <div className="text-xs font-semibold uppercase tracking-wide text-ink-faint">Variant types to add</div>
            <div className="mt-2 flex flex-wrap gap-x-4 gap-y-1.5">
              {variantTypes?.map((vt) => (
                <label key={vt.id} className="flex items-center gap-2 text-sm text-ink-soft">
                  <input
                    type="checkbox"
                    checked={selectedVariantTypeIds.has(vt.id)}
                    onChange={() => toggleVariantType(vt.id)}
                  />
                  {vt.name}
                </label>
              ))}
            </div>

            <div className="mt-3 flex flex-wrap items-center gap-2">
              <button
                type="button"
                disabled={!canRun || bulkAssign.isPending}
                onClick={handlePreview}
                className="rounded-lg border border-border px-3 py-1.5 text-xs font-semibold text-ink-soft hover:text-ink disabled:opacity-50"
              >
                Preview counts
              </button>
              <button
                type="button"
                disabled={!canRun || !preview || bulkAssign.isPending}
                onClick={handleApply}
                className="rounded-lg bg-accent px-3 py-1.5 text-xs font-semibold text-accent-ink disabled:opacity-50"
              >
                Apply
              </button>
            </div>

            {preview && !applied && (
              <div className="mt-3 rounded-lg bg-canvas p-3 text-xs text-ink-soft">
                Matches <span className="font-semibold text-ink">{preview.matchedCards}</span> card(s). Would create{' '}
                <span className="font-semibold text-ink">{preview.created}</span> variant row(s),{' '}
                {preview.skipped} already exist.
              </div>
            )}
            {applied && (
              <div className="mt-3 rounded-lg bg-good/10 p-3 text-xs text-ink">
                Done: created {applied.created}, skipped {applied.skipped} (already existed) across {applied.matchedCards}{' '}
                matched card(s).
              </div>
            )}
          </div>

          <div className="flex min-h-0 flex-1 flex-col rounded-xl border border-border p-3">
            <div className="mb-2 flex items-center justify-between text-xs font-semibold uppercase tracking-wide text-ink-faint">
              <span>Matching cards</span>
              <span className="normal-case text-ink-soft">
                {matchesPending ? 'Searching…' : `${(matches?.totalCount ?? 0).toLocaleString()} total`}
              </span>
            </div>
            <div className="h-[340px]">
              {matches && matches.items.length > 0 ? (
                <ResultsGrid results={matches.items} selectedIds={NO_SELECTION} onToggleSelect={() => {}} />
              ) : (
                <p className="p-4 text-xs text-ink-faint">
                  {matchesPending ? 'Searching…' : 'No cards match these filters yet — adjust filters on the left.'}
                </p>
              )}
            </div>
            {matches && matches.totalCount > matches.items.length && (
              <p className="mt-2 text-[10.5px] text-ink-faint">
                Showing the first {matches.items.length.toLocaleString()} of {matches.totalCount.toLocaleString()} — the
                bulk operation itself still applies to every matching card, not just what's shown here.
              </p>
            )}
          </div>
        </div>
      </div>
    </section>
  )
}
