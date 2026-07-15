import { useQuery } from '@tanstack/react-query'
import { api } from '../api'

export interface SetSummary {
  id: string
  name: string
  series: string
  printedTotal: number
  total: number
  releaseDate: string
  ptcgoCode: string | null
  symbolImageUrl: string | null
  logoImageUrl: string | null
  cardCount: number
  ownedCount: number
}

export interface VariantSummary {
  id: string
  variantTypeName: string
}

/** VariantSummary plus the current user's ownership of this specific variant. */
export interface OwnedVariantSummary {
  id: string
  variantTypeName: string
  owned: boolean
  quantity: number
  condition: string | null
}

export interface CardSummary {
  id: string
  setId: string
  name: string
  number: string
  rarity: string | null
  supertype: string
  imageSmallUrl: string | null
  imageLargeUrl: string | null
  variants: OwnedVariantSummary[]
}

export interface PagedResult<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
}

export interface Ability {
  name: string
  text: string
  type: string
}

export interface Attack {
  name: string
  cost: string[]
  convertedEnergyCost: number
  damage: string | null
  text: string
}

export interface TypeEffect {
  type: string
  value: string
}

export interface CardDetail {
  id: string
  setId: string
  name: string
  supertype: string
  subtypes: string[]
  level: string | null
  hp: string | null
  types: string[]
  evolvesFrom: string | null
  abilities: Ability[]
  attacks: Attack[]
  weaknesses: TypeEffect[]
  resistances: TypeEffect[]
  retreatCost: string[]
  convertedRetreatCost: number | null
  number: string
  artist: string | null
  rarity: string | null
  flavorText: string | null
  regulationMark: string | null
  nationalPokedexNumbers: number[]
  imageSmallUrl: string | null
  imageLargeUrl: string | null
  variants: OwnedVariantSummary[]
}

export function useSets() {
  return useQuery({
    queryKey: ['sets'],
    queryFn: async () => (await api.get<SetSummary[]>('/sets')).data,
  })
}

/** Every user (not just admins) can call this — it's how the search filter panel populates the variant filter. */
export function useVariantTypeNames() {
  return useQuery({
    queryKey: ['variant-type-names'],
    queryFn: async () => (await api.get<string[]>('/cards/variant-types')).data,
  })
}

export function useCard(cardId: string | null) {
  return useQuery({
    queryKey: ['card-detail', cardId],
    queryFn: async () => (await api.get<CardDetail>(`/cards/${cardId}`)).data,
    enabled: cardId !== null,
  })
}

/** Fetches every card in a set in one request, for the master-set checklist (largest real set is 304 cards). */
export function useFullSetCards(setId: string | null) {
  return useQuery({
    queryKey: ['set-cards-full', setId],
    queryFn: async () => (await api.get<PagedResult<CardSummary>>(`/sets/${setId}/cards`, { params: { pageSize: 500 } })).data,
    enabled: setId !== null,
  })
}
