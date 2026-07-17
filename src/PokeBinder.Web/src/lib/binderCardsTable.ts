import type { BinderCardRow } from './binder-cards-types'

export type SortColumn = 'cardName' | 'number' | 'setName' | 'releaseYear' | 'owned' | 'tagName'
export type SortDirection = 'asc' | 'desc'
export type HaveFilter = 'all' | 'have' | 'need'

function compareRows(a: BinderCardRow, b: BinderCardRow, column: SortColumn): number {
  switch (column) {
    case 'cardName':
      return a.cardName.localeCompare(b.cardName)
    case 'number':
      return a.number.localeCompare(b.number, undefined, { numeric: true })
    case 'setName':
      return a.setName.localeCompare(b.setName)
    case 'releaseYear':
      return (a.releaseYear ?? 0) - (b.releaseYear ?? 0)
    case 'owned':
      return Number(a.owned) - Number(b.owned)
    case 'tagName':
      return (a.tagName ?? '').localeCompare(b.tagName ?? '')
  }
}

export function filterAndSortBinderCards(
  rows: BinderCardRow[],
  haveFilter: HaveFilter,
  tagFilter: ReadonlySet<string> | null,
  sortColumn: SortColumn,
  sortDirection: SortDirection,
): BinderCardRow[] {
  let result = rows
  if (haveFilter === 'have') result = result.filter((r) => r.owned)
  if (haveFilter === 'need') result = result.filter((r) => !r.owned)
  if (tagFilter !== null) result = result.filter((r) => r.tagId !== null && tagFilter.has(r.tagId))

  return [...result].sort((a, b) => {
    const cmp = compareRows(a, b, sortColumn)
    return sortDirection === 'asc' ? cmp : -cmp
  })
}

/** Collects the distinct tags present across a binder's cards, for building the tag-filter chips. */
export function collectUniqueTags(rows: BinderCardRow[]): { id: string; name: string; colourHex: string }[] {
  const map = new Map<string, { id: string; name: string; colourHex: string }>()
  for (const r of rows) {
    if (r.tagId && r.tagName && r.tagColourHex) {
      map.set(r.tagId, { id: r.tagId, name: r.tagName, colourHex: r.tagColourHex })
    }
  }
  return Array.from(map.values())
}
