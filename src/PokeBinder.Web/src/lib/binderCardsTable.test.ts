import { describe, expect, it } from 'vitest'
import { collectUniqueTags, filterAndSortBinderCards } from './binderCardsTable'
import type { BinderCardRow } from './binder-cards-types'

function row(overrides: Partial<BinderCardRow>): BinderCardRow {
  return {
    slotId: 'slot',
    pageNumber: 1,
    position: 0,
    cardId: 'card',
    cardName: 'Card',
    setId: 'set',
    setName: 'Set',
    number: '1',
    releaseYear: 2000,
    owned: false,
    tagId: null,
    tagName: null,
    tagColourHex: null,
    ...overrides,
  }
}

describe('filterAndSortBinderCards', () => {
  it('filters to only owned cards when haveFilter is "have"', () => {
    const rows = [row({ slotId: 'a', owned: true }), row({ slotId: 'b', owned: false })]
    const result = filterAndSortBinderCards(rows, 'have', null, 'cardName', 'asc')
    expect(result.map((r) => r.slotId)).toEqual(['a'])
  })

  it('filters to only missing cards when haveFilter is "need"', () => {
    const rows = [row({ slotId: 'a', owned: true }), row({ slotId: 'b', owned: false })]
    const result = filterAndSortBinderCards(rows, 'need', null, 'cardName', 'asc')
    expect(result.map((r) => r.slotId)).toEqual(['b'])
  })

  it('filters by tag when a tag filter set is provided', () => {
    const rows = [
      row({ slotId: 'a', tagId: 'tag-1', tagName: 'Ordered', tagColourHex: '#fff' }),
      row({ slotId: 'b', tagId: 'tag-2', tagName: 'Trade', tagColourHex: '#000' }),
      row({ slotId: 'c', tagId: null }),
    ]
    const result = filterAndSortBinderCards(rows, 'all', new Set(['tag-1']), 'cardName', 'asc')
    expect(result.map((r) => r.slotId)).toEqual(['a'])
  })

  it('sorts by card number using numeric comparison, not lexicographic', () => {
    const rows = [row({ slotId: 'a', number: '10' }), row({ slotId: 'b', number: '2' })]
    const result = filterAndSortBinderCards(rows, 'all', null, 'number', 'asc')
    // Lexicographic sort would put "10" before "2"; numeric sort should not.
    expect(result.map((r) => r.slotId)).toEqual(['b', 'a'])
  })

  it('reverses order when sortDirection is desc', () => {
    const rows = [row({ slotId: 'a', cardName: 'Alpha' }), row({ slotId: 'b', cardName: 'Beta' })]
    const result = filterAndSortBinderCards(rows, 'all', null, 'cardName', 'desc')
    expect(result.map((r) => r.slotId)).toEqual(['b', 'a'])
  })

  it('combines have and tag filters together', () => {
    const rows = [
      row({ slotId: 'a', owned: true, tagId: 'tag-1', tagName: 'Ordered', tagColourHex: '#fff' }),
      row({ slotId: 'b', owned: false, tagId: 'tag-1', tagName: 'Ordered', tagColourHex: '#fff' }),
    ]
    const result = filterAndSortBinderCards(rows, 'have', new Set(['tag-1']), 'cardName', 'asc')
    expect(result.map((r) => r.slotId)).toEqual(['a'])
  })
})

describe('collectUniqueTags', () => {
  it('returns each distinct tag once, ignoring untagged rows', () => {
    const rows = [
      row({ tagId: 'tag-1', tagName: 'Ordered', tagColourHex: '#fff' }),
      row({ tagId: 'tag-1', tagName: 'Ordered', tagColourHex: '#fff' }),
      row({ tagId: 'tag-2', tagName: 'Trade', tagColourHex: '#000' }),
      row({ tagId: null }),
    ]
    const result = collectUniqueTags(rows)
    expect(result).toEqual([
      { id: 'tag-1', name: 'Ordered', colourHex: '#fff' },
      { id: 'tag-2', name: 'Trade', colourHex: '#000' },
    ])
  })
})
