import { useMemo, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { useBinder } from '../lib/queries/binders'
import { useBinderCards } from '../lib/queries/binderCards'
import { collectUniqueTags, filterAndSortBinderCards, type HaveFilter, type SortColumn, type SortDirection } from '../lib/binderCardsTable'

const COLUMNS: { key: SortColumn; label: string }[] = [
  { key: 'cardName', label: 'Card' },
  { key: 'number', label: 'Number' },
  { key: 'setName', label: 'Set' },
  { key: 'releaseYear', label: 'Year' },
  { key: 'owned', label: 'Have / Need' },
  { key: 'tagName', label: 'Tag' },
]

export function BinderTablePage() {
  const { id } = useParams<{ id: string }>()
  const binderId = id!
  const { data: binder } = useBinder(binderId)
  const { data: rows, isPending } = useBinderCards(binderId)

  const [haveFilter, setHaveFilter] = useState<HaveFilter>('all')
  const [tagFilter, setTagFilter] = useState<Set<string> | null>(null)
  const [sortColumn, setSortColumn] = useState<SortColumn>('setName')
  const [sortDirection, setSortDirection] = useState<SortDirection>('asc')
  const [exporting, setExporting] = useState<'pdf' | 'word' | null>(null)

  const uniqueTags = useMemo(() => collectUniqueTags(rows ?? []), [rows])

  const filteredSortedRows = useMemo(
    () => filterAndSortBinderCards(rows ?? [], haveFilter, tagFilter, sortColumn, sortDirection),
    [rows, haveFilter, tagFilter, sortColumn, sortDirection],
  )

  function handleSort(column: SortColumn) {
    if (sortColumn === column) {
      setSortDirection((d) => (d === 'asc' ? 'desc' : 'asc'))
    } else {
      setSortColumn(column)
      setSortDirection('asc')
    }
  }

  function toggleTag(tagId: string) {
    setTagFilter((prev) => {
      const all = new Set(uniqueTags.map((t) => t.id))
      const current = prev ?? all
      const next = new Set(current)
      if (next.has(tagId)) next.delete(tagId)
      else next.add(tagId)
      return next.size === all.size ? null : next
    })
  }

  const binderName = binder?.name ?? 'Binder'

  // jsPDF/docx are only pulled into the bundle once someone actually exports — dynamically
  // imported here rather than statically, since jsPDF alone drags in html2canvas (~200kB) that
  // otherwise ships to every visitor even though most will never click these buttons.
  async function handleExportPdf() {
    setExporting('pdf')
    try {
      const { exportBinderCardsToPdf } = await import('../lib/export/exportPdf')
      exportBinderCardsToPdf(binderName, filteredSortedRows)
    } finally {
      setExporting(null)
    }
  }

  async function handleExportWord() {
    setExporting('word')
    try {
      const { exportBinderCardsToWord } = await import('../lib/export/exportWord')
      await exportBinderCardsToWord(binderName, filteredSortedRows)
    } finally {
      setExporting(null)
    }
  }

  return (
    <div className="mx-auto max-w-5xl">
      <div className="rounded-t-[14px] border border-border bg-surface p-3.5 sm:p-4">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <div>
            <Link to={`/binders/${binderId}`} className="text-xs font-semibold text-ink-soft hover:text-ink">
              ← Back to binder
            </Link>
            <h1 className="mt-1 font-display text-lg italic text-ink">{binderName} — Card list</h1>
          </div>
          <div className="flex flex-wrap items-center gap-2">
            <button
              type="button"
              onClick={handleExportPdf}
              disabled={filteredSortedRows.length === 0 || exporting !== null}
              className="rounded-lg border border-border px-2.5 py-1.5 text-[11px] font-semibold text-ink-soft hover:text-ink disabled:opacity-40"
            >
              {exporting === 'pdf' ? 'Preparing…' : 'Export PDF'}
            </button>
            <button
              type="button"
              onClick={handleExportWord}
              disabled={filteredSortedRows.length === 0 || exporting !== null}
              className="rounded-lg border border-border px-2.5 py-1.5 text-[11px] font-semibold text-ink-soft hover:text-ink disabled:opacity-40"
            >
              {exporting === 'word' ? 'Preparing…' : 'Export Word'}
            </button>
          </div>
        </div>

        <div className="mt-3 flex flex-wrap items-center gap-3 border-t border-border pt-3">
          <div className="flex items-center gap-1">
            {(['all', 'have', 'need'] as const).map((option) => (
              <button
                key={option}
                type="button"
                onClick={() => setHaveFilter(option)}
                aria-pressed={haveFilter === option}
                className={`rounded-lg border px-2 py-1 text-[10.5px] font-semibold capitalize ${
                  haveFilter === option ? 'border-accent text-accent' : 'border-border text-ink-soft'
                }`}
              >
                {option === 'all' ? 'Show all' : option === 'have' ? 'Have' : 'Need'}
              </button>
            ))}
          </div>

          {uniqueTags.length > 0 && (
            <div className="flex flex-wrap items-center gap-1.5">
              {uniqueTags.map((tag) => {
                const visible = tagFilter === null || tagFilter.has(tag.id)
                return (
                  <button
                    key={tag.id}
                    type="button"
                    onClick={() => toggleTag(tag.id)}
                    aria-pressed={visible}
                    className={`flex items-center gap-1.5 rounded-lg border px-2 py-1 text-[10.5px] font-semibold ${
                      visible ? 'border-border text-ink-soft' : 'border-border text-ink-faint opacity-40'
                    }`}
                  >
                    <span className="h-2.5 w-2.5 rounded-sm" style={{ background: tag.colourHex }} />
                    {tag.name}
                  </button>
                )
              })}
              {tagFilter !== null && (
                <button type="button" onClick={() => setTagFilter(null)} className="text-[10.5px] font-semibold text-accent">
                  Show all tags
                </button>
              )}
            </div>
          )}

          <span className="text-[10.5px] text-ink-faint">
            {filteredSortedRows.length.toLocaleString()} card{filteredSortedRows.length === 1 ? '' : 's'}
          </span>
        </div>
      </div>

      <div className="overflow-x-auto rounded-b-[14px] border border-t-0 border-border bg-surface-2">
        <table className="w-full min-w-[640px] text-left text-sm">
          <thead>
            <tr className="border-b border-border text-xs uppercase tracking-wide text-ink-faint">
              {COLUMNS.map((col) => (
                <th key={col.key} className="py-2.5 px-3 font-semibold">
                  <button type="button" onClick={() => handleSort(col.key)} className="flex items-center gap-1 hover:text-ink">
                    {col.label}
                    {sortColumn === col.key && <span>{sortDirection === 'asc' ? '↑' : '↓'}</span>}
                  </button>
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {filteredSortedRows.map((row) => (
              <tr key={row.slotId} className="border-b border-border/60">
                <td className="px-3 py-2 text-ink">{row.cardName}</td>
                <td className="px-3 py-2 text-ink-soft [font-variant-numeric:tabular-nums]">{row.number}</td>
                <td className="px-3 py-2 text-ink-soft">{row.setName}</td>
                <td className="px-3 py-2 text-ink-soft [font-variant-numeric:tabular-nums]">{row.releaseYear ?? '—'}</td>
                <td className="px-3 py-2">
                  <span className={row.owned ? 'font-semibold text-good' : 'font-semibold text-bad'}>
                    {row.owned ? 'Have' : 'Need'}
                  </span>
                </td>
                <td className="px-3 py-2">
                  {row.tagName ? (
                    <span className="inline-flex items-center gap-1.5 text-xs text-ink-soft">
                      <span className="h-2.5 w-2.5 rounded-sm" style={{ background: row.tagColourHex ?? undefined }} />
                      {row.tagName}
                    </span>
                  ) : (
                    <span className="text-xs text-ink-faint">—</span>
                  )}
                </td>
              </tr>
            ))}
            {!isPending && filteredSortedRows.length === 0 && (
              <tr>
                <td colSpan={COLUMNS.length} className="py-6 text-center text-xs text-ink-faint">
                  No cards match these filters.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  )
}
