import type { CardSummary } from './queries/cards'

export interface RarityGroup {
  rarity: string
  cards: CardSummary[]
}

export interface SupertypeGroup {
  supertype: string
  rarityGroups: RarityGroup[]
}

const SUPERTYPE_ORDER = ['Pokémon', 'Trainer', 'Energy']

/** Groups a set's cards by supertype (Pokémon/Trainer/Energy order), then by rarity within each, preserving the incoming (numberSortKey) order inside each group. */
export function groupSetCards(cards: CardSummary[]): SupertypeGroup[] {
  const bySupertype = new Map<string, CardSummary[]>()
  for (const card of cards) {
    const list = bySupertype.get(card.supertype)
    if (list) {
      list.push(card)
    } else {
      bySupertype.set(card.supertype, [card])
    }
  }

  const supertypes = [...bySupertype.keys()].sort((a, b) => {
    const ai = SUPERTYPE_ORDER.indexOf(a)
    const bi = SUPERTYPE_ORDER.indexOf(b)
    return (ai === -1 ? SUPERTYPE_ORDER.length : ai) - (bi === -1 ? SUPERTYPE_ORDER.length : bi)
  })

  return supertypes.map((supertype) => {
    const cardsForSupertype = bySupertype.get(supertype)!
    const byRarity = new Map<string, CardSummary[]>()
    for (const card of cardsForSupertype) {
      const rarity = card.rarity ?? 'No rarity listed'
      const list = byRarity.get(rarity)
      if (list) {
        list.push(card)
      } else {
        byRarity.set(rarity, [card])
      }
    }

    return {
      supertype,
      rarityGroups: [...byRarity.entries()].map(([rarity, rarityCards]) => ({ rarity, cards: rarityCards })),
    }
  })
}
