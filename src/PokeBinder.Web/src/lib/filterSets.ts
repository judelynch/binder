import type { SetSummary } from './queries/cards'

export type SetSortOrder = 'releaseDateDesc' | 'releaseDateAsc' | 'nameAsc' | 'completionDesc'

export interface SetFilters {
  query: string
  series: string | null
  sort: SetSortOrder
}

export const EMPTY_SET_FILTERS: SetFilters = { query: '', series: null, sort: 'releaseDateDesc' }

export function completionRatio(set: SetSummary): number {
  return set.cardCount === 0 ? 0 : set.ownedCount / set.cardCount
}

/** Client-side filter + sort over the full (already-fetched) sets list — the catalog is small enough that a server round-trip for this would be pure overhead. */
export function filterAndSortSets(sets: SetSummary[], filters: SetFilters): SetSummary[] {
  const query = filters.query.trim().toLowerCase()

  const filtered = sets.filter((set) => {
    if (query && !set.name.toLowerCase().includes(query)) return false
    if (filters.series && set.series !== filters.series) return false
    return true
  })

  return [...filtered].sort((a, b) => {
    switch (filters.sort) {
      case 'nameAsc':
        return a.name.localeCompare(b.name)
      case 'releaseDateAsc':
        return a.releaseDate.localeCompare(b.releaseDate)
      case 'completionDesc':
        return completionRatio(b) - completionRatio(a)
      case 'releaseDateDesc':
      default:
        return b.releaseDate.localeCompare(a.releaseDate)
    }
  })
}

/** Distinct series names present in the catalog, alphabetical, for the series filter dropdown. */
export function uniqueSeries(sets: SetSummary[]): string[] {
  return [...new Set(sets.map((s) => s.series))].sort((a, b) => a.localeCompare(b))
}

/**
 * The `limit` sets closest to completion but not yet finished, for the home page's "keep going"
 * prompt — a 100%-complete set has nothing left to work on, so it's excluded rather than sitting
 * at the top of the list forever.
 */
export function topInProgressSets(sets: SetSummary[], limit: number): SetSummary[] {
  return sets
    .filter((s) => s.ownedCount > 0 && s.ownedCount < s.cardCount)
    .sort((a, b) => completionRatio(b) - completionRatio(a))
    .slice(0, limit)
}
