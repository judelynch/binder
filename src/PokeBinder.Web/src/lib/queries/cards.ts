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
}

export interface VariantSummary {
  id: string
  variantTypeName: string
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
  variants: VariantSummary[]
}

export interface PagedResult<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
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
  number: string
  artist: string | null
  rarity: string | null
  flavorText: string | null
  regulationMark: string | null
  nationalPokedexNumbers: number[]
  imageSmallUrl: string | null
  imageLargeUrl: string | null
  variantTypeNames: string[]
}

export function useSets() {
  return useQuery({
    queryKey: ['sets'],
    queryFn: async () => (await api.get<SetSummary[]>('/sets')).data,
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
