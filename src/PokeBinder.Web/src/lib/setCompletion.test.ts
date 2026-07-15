import { describe, expect, it } from 'vitest'
import { computeSetCompletion, isCardComplete } from './setCompletion'
import type { CardSummary, OwnedVariantSummary } from './queries/cards'

function variant(overrides: Partial<OwnedVariantSummary>): OwnedVariantSummary {
  return { id: 'v', variantTypeName: 'Normal', owned: false, quantity: 0, condition: null, ...overrides }
}

function card(overrides: Partial<CardSummary>): CardSummary {
  return {
    id: 'c',
    setId: 'set',
    name: 'Card',
    number: '1',
    rarity: 'Common',
    supertype: 'Pokémon',
    imageSmallUrl: null,
    imageLargeUrl: null,
    variants: [],
    ...overrides,
  }
}

describe('isCardComplete', () => {
  it('is complete once every non-Stamp variant is owned, even if the Stamp variant is not', () => {
    const c = card({
      variants: [
        variant({ id: 'normal', variantTypeName: 'Normal', owned: true }),
        variant({ id: 'rh', variantTypeName: 'Reverse Holo', owned: true }),
        variant({ id: 'stamp', variantTypeName: 'Promo Stamp', owned: false }),
      ],
    })
    expect(isCardComplete(c)).toBe(true)
  })

  it('is incomplete when a non-Stamp variant is unowned', () => {
    const c = card({
      variants: [variant({ id: 'normal', variantTypeName: 'Normal', owned: true }), variant({ id: 'rh', variantTypeName: 'Reverse Holo', owned: false })],
    })
    expect(isCardComplete(c)).toBe(false)
  })

  it('is vacuously complete when the only variant is a Stamp variant', () => {
    const c = card({ variants: [variant({ id: 'stamp', variantTypeName: 'Promo Stamp', owned: false })] })
    expect(isCardComplete(c)).toBe(true)
  })

  it('excludes Stamp variants case-insensitively', () => {
    const c = card({ variants: [variant({ id: 'stamp', variantTypeName: 'STAMP', owned: false })] })
    expect(isCardComplete(c)).toBe(true)
  })
})

describe('computeSetCompletion', () => {
  it('counts complete cards against the total', () => {
    const complete = card({ id: 'a', variants: [variant({ owned: true })] })
    const incomplete = card({ id: 'b', variants: [variant({ owned: false })] })
    const result = computeSetCompletion([complete, incomplete])
    expect(result).toEqual({ ownedCount: 1, totalCount: 2 })
  })
})
