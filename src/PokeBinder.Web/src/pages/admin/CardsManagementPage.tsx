import { useState } from 'react'
import { SearchFilterPanel } from '../../components/search/SearchFilterPanel'
import { CardImage } from '../../components/binder/CardImage'
import { AddCardModal } from '../../components/admin/AddCardModal'
import { AddSetModal } from '../../components/admin/AddSetModal'
import { EditCardModal } from '../../components/admin/EditCardModal'
import { useCardSearch } from '../../lib/queries/search'
import { EMPTY_FILTERS } from '../../lib/search-types'

export function CardsManagementPage() {
  const [filters, setFilters] = useState(EMPTY_FILTERS)
  const [page, setPage] = useState(1)
  const [editingCardId, setEditingCardId] = useState<string | null>(null)
  const [addingSet, setAddingSet] = useState(false)
  const [addingCard, setAddingCard] = useState(false)

  const { data, isPending } = useCardSearch(filters, page)

  return (
    <div className="grid grid-cols-1 gap-5 lg:grid-cols-[280px_1fr]">
      <div className="rounded-2xl border border-border bg-surface lg:h-[calc(100vh-14rem)]">
        <SearchFilterPanel
          filters={filters}
          onChange={(next) => {
            setFilters(next)
            setPage(1)
          }}
          resultCount={data?.totalCount ?? null}
        />
      </div>

      <div>
        <div className="mb-3 flex justify-end gap-2">
          <button
            type="button"
            onClick={() => setAddingSet(true)}
            className="rounded-lg border border-border px-3 py-1.5 text-xs font-semibold text-ink-soft hover:text-ink"
          >
            + Add set
          </button>
          <button
            type="button"
            onClick={() => setAddingCard(true)}
            className="rounded-lg border border-border px-3 py-1.5 text-xs font-semibold text-ink-soft hover:text-ink"
          >
            + Add card
          </button>
        </div>

        <div className="overflow-hidden rounded-2xl border border-border bg-surface">
          <table className="w-full text-left text-sm">
            <thead>
              <tr className="border-b border-border text-xs uppercase tracking-wide text-ink-faint">
                <th className="py-2 pl-4 pr-3 font-semibold" />
                <th className="py-2 pr-3 font-semibold">Name</th>
                <th className="py-2 pr-3 font-semibold">Set</th>
                <th className="py-2 pr-3 font-semibold">#</th>
                <th className="py-2 pr-3 font-semibold">Rarity</th>
                <th className="py-2 pr-4 font-semibold" />
              </tr>
            </thead>
            <tbody>
              {data?.items.map((card) => (
                <tr key={card.id} className="border-b border-border/60">
                  <td className="py-2 pl-4 pr-3">
                    <div className="relative h-12 w-9 overflow-hidden rounded bg-canvas">
                      <CardImage src={card.imageSmallUrl} alt={card.name} greyscale={false} />
                    </div>
                  </td>
                  <td className="py-2 pr-3 text-ink">{card.name}</td>
                  <td className="py-2 pr-3 text-ink-soft">{card.setName}</td>
                  <td className="py-2 pr-3 text-ink-soft [font-variant-numeric:tabular-nums]">{card.number}</td>
                  <td className="py-2 pr-3 text-ink-soft">{card.rarity ?? '—'}</td>
                  <td className="py-2 pr-4 text-right">
                    <button
                      type="button"
                      onClick={() => setEditingCardId(card.id)}
                      className="rounded border border-border px-2 py-1 text-xs text-ink-soft hover:text-ink"
                    >
                      Edit
                    </button>
                  </td>
                </tr>
              ))}
              {!isPending && data?.items.length === 0 && (
                <tr>
                  <td colSpan={6} className="py-6 text-center text-xs text-ink-faint">
                    No cards match these filters.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>

        {data && data.totalCount > data.pageSize && (
          <div className="mt-3 flex items-center justify-end gap-2 text-xs">
            <button
              type="button"
              disabled={page <= 1}
              onClick={() => setPage((p) => p - 1)}
              className="rounded border border-border px-2 py-1 disabled:opacity-30"
            >
              Prev
            </button>
            <span className="text-ink-faint">
              Page {page} of {Math.ceil(data.totalCount / data.pageSize)}
            </span>
            <button
              type="button"
              disabled={page * data.pageSize >= data.totalCount}
              onClick={() => setPage((p) => p + 1)}
              className="rounded border border-border px-2 py-1 disabled:opacity-30"
            >
              Next
            </button>
          </div>
        )}
      </div>

      {editingCardId && <EditCardModal cardId={editingCardId} onClose={() => setEditingCardId(null)} />}
      {addingSet && <AddSetModal onClose={() => setAddingSet(false)} />}
      {addingCard && <AddCardModal onClose={() => setAddingCard(false)} />}
    </div>
  )
}
