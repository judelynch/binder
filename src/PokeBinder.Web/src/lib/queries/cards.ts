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

export function useSets() {
  return useQuery({
    queryKey: ['sets'],
    queryFn: async () => (await api.get<SetSummary[]>('/sets')).data,
  })
}

export function useSetCards(setId: string | null, name: string) {
  return useQuery({
    queryKey: ['set-cards', setId, name],
    queryFn: async () =>
      (
        await api.get<PagedResult<CardSummary>>(`/sets/${setId}/cards`, {
          params: { name: name || undefined, pageSize: 30 },
        })
      ).data,
    enabled: setId !== null,
  })
}
