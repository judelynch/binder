import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { api } from '../api'
import type { PagedResult } from './cards'
import type { CardSearchFilters, CardSearchResult } from '../search-types'

export const SELECT_ALL_CAP = 500

function buildParams(filters: CardSearchFilters, page: number, pageSize: number) {
  const params: Record<string, string | number | boolean | string[]> = {
    page,
    pageSize,
    sort: filters.sort,
    sortDescending: filters.sortDescending,
  }

  if (filters.name) params.name = filters.name
  if (filters.supertype) params.supertype = filters.supertype
  if (filters.subtypes.length) params.subtypes = filters.subtypes
  if (filters.types.length) params.types = filters.types
  if (filters.setIds.length) params.setIds = filters.setIds
  if (filters.series.length) params.series = filters.series
  if (filters.rarities.length) params.rarities = filters.rarities
  if (filters.hpMin !== null) params.hpMin = filters.hpMin
  if (filters.hpMax !== null) params.hpMax = filters.hpMax
  if (filters.weaknessType) params.weaknessType = filters.weaknessType
  if (filters.resistanceType) params.resistanceType = filters.resistanceType
  if (filters.retreatCostMin !== null) params.retreatCostMin = filters.retreatCostMin
  if (filters.retreatCostMax !== null) params.retreatCostMax = filters.retreatCostMax
  if (filters.artist) params.artist = filters.artist
  if (filters.regulationMarks.length) params.regulationMarks = filters.regulationMarks
  if (filters.nationalPokedexNumber !== null) params.nationalPokedexNumber = filters.nationalPokedexNumber
  if (filters.variantTypes.length) params.variantTypes = filters.variantTypes
  if (filters.hasPriceData) params.hasPriceData = true
  if (filters.priceMin !== null) params.priceMin = filters.priceMin
  if (filters.priceMax !== null) params.priceMax = filters.priceMax

  return params
}

export function useCardSearch(filters: CardSearchFilters, page: number, pageSize = 60) {
  return useQuery({
    queryKey: ['card-search', filters, page, pageSize],
    queryFn: async () =>
      (await api.get<PagedResult<CardSearchResult>>('/cards/search', { params: buildParams(filters, page, pageSize) })).data,
    placeholderData: keepPreviousData,
  })
}

/** Fetches up to SELECT_ALL_CAP results for the current filters in one shot, for "select all results". */
export function useSelectAllResults(filters: CardSearchFilters, enabled: boolean) {
  return useQuery({
    queryKey: ['card-search-select-all', filters],
    queryFn: async () =>
      (await api.get<PagedResult<CardSearchResult>>('/cards/search', { params: buildParams(filters, 1, SELECT_ALL_CAP) })).data,
    enabled,
  })
}
