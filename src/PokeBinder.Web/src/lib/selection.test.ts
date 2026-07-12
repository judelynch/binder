import { describe, expect, it } from 'vitest'
import { capSelection, toggleSelection } from './selection'

describe('capSelection', () => {
  it('returns everything uncapped when under the limit', () => {
    const result = capSelection(['a', 'b', 'c'], 500)
    expect(result).toEqual({ ids: ['a', 'b', 'c'], wasCapped: false, totalAvailable: 3 })
  })

  it('caps at the limit and reports it was capped', () => {
    const ids = Array.from({ length: 600 }, (_, i) => `id-${i}`)
    const result = capSelection(ids, 500)
    expect(result.ids).toHaveLength(500)
    expect(result.wasCapped).toBe(true)
    expect(result.totalAvailable).toBe(600)
  })

  it('is not capped exactly at the limit', () => {
    const ids = Array.from({ length: 500 }, (_, i) => `id-${i}`)
    const result = capSelection(ids, 500)
    expect(result.wasCapped).toBe(false)
    expect(result.ids).toHaveLength(500)
  })
})

describe('toggleSelection', () => {
  it('adds an id not yet selected', () => {
    const next = toggleSelection(new Set(['a']), 'b')
    expect([...next].sort()).toEqual(['a', 'b'])
  })

  it('removes an id already selected', () => {
    const next = toggleSelection(new Set(['a', 'b']), 'a')
    expect([...next]).toEqual(['b'])
  })

  it('does not mutate the original set', () => {
    const original = new Set(['a'])
    toggleSelection(original, 'b')
    expect(original.has('b')).toBe(false)
  })
})
