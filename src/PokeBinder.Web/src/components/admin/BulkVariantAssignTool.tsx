import { useState } from 'react'
import { SearchFilterPanel } from '../search/SearchFilterPanel'
import { useBulkAssignVariants, useVariantTypes } from '../../lib/queries/admin'
import { EMPTY_FILTERS } from '../../lib/search-types'
import type { BulkVariantAssignResult } from '../../lib/admin-types'

export function BulkVariantAssignTool() {
  const [filters, setFilters] = useState(EMPTY_FILTERS)
  const [selectedVariantTypeIds, setSelectedVariantTypeIds] = useState<Set<string>>(new Set())
  const [preview, setPreview] = useState<BulkVariantAssignResult | null>(null)
  const [applied, setApplied] = useState<BulkVariantAssignResult | null>(null)

  const { data: variantTypes } = useVariantTypes()
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

      <div className="mt-4 grid grid-cols-1 gap-4 lg:grid-cols-[1fr_260px]">
        <div className="rounded-xl border border-border">
          <div className="max-h-[420px] overflow-y-auto">
            <SearchFilterPanel
              filters={filters}
              onChange={(next) => {
                setFilters(next)
                setPreview(null)
                setApplied(null)
              }}
              resultCount={null}
            />
          </div>
        </div>

        <div>
          <div className="text-xs font-semibold uppercase tracking-wide text-ink-faint">Variant types to add</div>
          <div className="mt-2 space-y-1.5">
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

          <div className="mt-4 space-y-2">
            <button
              type="button"
              disabled={!canRun || bulkAssign.isPending}
              onClick={handlePreview}
              className="w-full rounded-lg border border-border py-2 text-sm font-semibold text-ink-soft hover:text-ink disabled:opacity-50"
            >
              Preview
            </button>
            <button
              type="button"
              disabled={!canRun || !preview || bulkAssign.isPending}
              onClick={handleApply}
              className="w-full rounded-lg bg-accent py-2 text-sm font-semibold text-accent-ink disabled:opacity-50"
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
      </div>
    </section>
  )
}
