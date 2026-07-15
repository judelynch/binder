import { describe, expect, it } from 'vitest'
import { EMPTY_SET_FILTERS, filterAndSortSets, topInProgressSets, uniqueSeries } from './filterSets'
import type { SetSummary } from './queries/cards'

function set(overrides: Partial<SetSummary>): SetSummary {
  return {
    id: 'x',
    name: 'Base Set',
    series: 'Base',
    printedTotal: 102,
    total: 102,
    releaseDate: '1999-01-09',
    ptcgoCode: null,
    symbolImageUrl: null,
    logoImageUrl: null,
    cardCount: 102,
    ownedCount: 0,
    ...overrides,
  }
}

describe('filterAndSortSets', () => {
  it('filters by name, case-insensitively', () => {
    const sets = [set({ id: 'a', name: 'Base Set' }), set({ id: 'b', name: 'Jungle' })]
    const result = filterAndSortSets(sets, { ...EMPTY_SET_FILTERS, query: 'base' })
    expect(result.map((s) => s.id)).toEqual(['a'])
  })

  it('filters by series', () => {
    const sets = [set({ id: 'a', series: 'Base' }), set({ id: 'b', series: 'Sword & Shield' })]
    const result = filterAndSortSets(sets, { ...EMPTY_SET_FILTERS, series: 'Sword & Shield' })
    expect(result.map((s) => s.id)).toEqual(['b'])
  })

  it('sorts by release date descending by default', () => {
    const sets = [
      set({ id: 'old', releaseDate: '1999-01-09' }),
      set({ id: 'new', releaseDate: '2023-03-31' }),
    ]
    const result = filterAndSortSets(sets, EMPTY_SET_FILTERS)
    expect(result.map((s) => s.id)).toEqual(['new', 'old'])
  })

  it('sorts by name ascending', () => {
    const sets = [set({ id: 'b', name: 'Jungle' }), set({ id: 'a', name: 'Base Set' })]
    const result = filterAndSortSets(sets, { ...EMPTY_SET_FILTERS, sort: 'nameAsc' })
    expect(result.map((s) => s.id)).toEqual(['a', 'b'])
  })

  it('sorts by completion percentage descending', () => {
    const sets = [
      set({ id: 'low', cardCount: 100, ownedCount: 10 }),
      set({ id: 'high', cardCount: 100, ownedCount: 90 }),
      set({ id: 'empty', cardCount: 0, ownedCount: 0 }),
    ]
    const result = filterAndSortSets(sets, { ...EMPTY_SET_FILTERS, sort: 'completionDesc' })
    expect(result.map((s) => s.id)).toEqual(['high', 'low', 'empty'])
  })
})

describe('uniqueSeries', () => {
  it('returns distinct series names, alphabetically', () => {
    const sets = [set({ series: 'Sword & Shield' }), set({ series: 'Base' }), set({ series: 'Base' })]
    expect(uniqueSeries(sets)).toEqual(['Base', 'Sword & Shield'])
  })
})

describe('topInProgressSets', () => {
  it('excludes sets with zero owned cards and sets that are already 100% complete', () => {
    const sets = [
      set({ id: 'untouched', cardCount: 100, ownedCount: 0 }),
      set({ id: 'finished', cardCount: 100, ownedCount: 100 }),
      set({ id: 'inProgress', cardCount: 100, ownedCount: 50 }),
    ]
    expect(topInProgressSets(sets, 3).map((s) => s.id)).toEqual(['inProgress'])
  })

  it('sorts by completion percentage descending and respects the limit', () => {
    const sets = [
      set({ id: 'a', cardCount: 100, ownedCount: 10 }),
      set({ id: 'b', cardCount: 100, ownedCount: 90 }),
      set({ id: 'c', cardCount: 100, ownedCount: 50 }),
    ]
    expect(topInProgressSets(sets, 2).map((s) => s.id)).toEqual(['b', 'c'])
  })
})
