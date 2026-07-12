import { describe, expect, it } from 'vitest'
import { groupSetCards } from './groupSetCards'
import type { CardSummary } from './queries/cards'

function card(overrides: Partial<CardSummary>): CardSummary {
  return {
    id: 'x',
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

describe('groupSetCards', () => {
  it('orders supertype groups as Pokémon, Trainer, Energy', () => {
    const cards = [
      card({ id: 'e1', supertype: 'Energy' }),
      card({ id: 't1', supertype: 'Trainer' }),
      card({ id: 'p1', supertype: 'Pokémon' }),
    ]
    const groups = groupSetCards(cards)
    expect(groups.map((g) => g.supertype)).toEqual(['Pokémon', 'Trainer', 'Energy'])
  })

  it('groups by rarity within a supertype, preserving input order', () => {
    const cards = [
      card({ id: 'a', rarity: 'Common', number: '1' }),
      card({ id: 'b', rarity: 'Rare Holo', number: '2' }),
      card({ id: 'c', rarity: 'Common', number: '3' }),
    ]
    const groups = groupSetCards(cards)
    const pokemon = groups.find((g) => g.supertype === 'Pokémon')!
    const commonGroup = pokemon.rarityGroups.find((r) => r.rarity === 'Common')!
    expect(commonGroup.cards.map((c) => c.id)).toEqual(['a', 'c'])
  })

  it('falls back to "No rarity listed" for cards with a null rarity', () => {
    const groups = groupSetCards([card({ id: 'x', rarity: null })])
    expect(groups[0]!.rarityGroups[0]!.rarity).toBe('No rarity listed')
  })

  it('omits supertypes with no cards', () => {
    const groups = groupSetCards([card({ supertype: 'Trainer' })])
    expect(groups.map((g) => g.supertype)).toEqual(['Trainer'])
  })
})
