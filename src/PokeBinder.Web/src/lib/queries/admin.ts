import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '../api'
import type {
  BulkVariantAssignResult,
  CardEditAudit,
  SyncDiffSummary,
  SyncRun,
  VariantType,
} from '../admin-types'
import type { CardSearchFilters } from '../search-types'
import type { PagedResult } from './cards'

// ---- Sync ----

export function useDryRunSync() {
  return useMutation({
    mutationFn: async () => (await api.post<SyncDiffSummary>('/admin/sync/dry-run')).data,
  })
}

export function useApplySync() {
  return useMutation({
    mutationFn: async (confirmed: { confirmedOverrideCardIds?: string[]; confirmedOverrideSetIds?: string[] }) =>
      (await api.post<{ jobId: string }>('/admin/sync/apply', confirmed)).data,
  })
}

export function useSyncJob(jobId: string | null) {
  return useQuery({
    queryKey: ['admin-sync-job', jobId],
    queryFn: async () => (await api.get<SyncRun>(`/admin/sync/jobs/${jobId}`)).data,
    enabled: jobId !== null,
    refetchInterval: (query) => (query.state.data?.status === 'Running' ? 500 : false),
  })
}

export function useSyncHistory(page: number, pageSize = 20) {
  return useQuery({
    queryKey: ['admin-sync-history', page, pageSize],
    queryFn: async () => (await api.get<PagedResult<SyncRun>>('/admin/sync/history', { params: { page, pageSize } })).data,
  })
}

// ---- Card / set management ----

export interface UpdateCardPayload {
  name?: string
  rarity?: string
  artist?: string
  flavorText?: string
  regulationMark?: string
  imageSmallUrl?: string
  imageLargeUrl?: string
  auditNote: string
}

export function useUpdateCard(cardId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async (payload: UpdateCardPayload) => (await api.put(`/admin/cards/${cardId}`, payload)).data,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['card-search'] })
      queryClient.invalidateQueries({ queryKey: ['set-cards-full'] })
      queryClient.invalidateQueries({ queryKey: ['card-audit', cardId] })
    },
  })
}

export function useCardAudit(cardId: string | null) {
  return useQuery({
    queryKey: ['card-audit', cardId],
    queryFn: async () => (await api.get<CardEditAudit[]>(`/admin/cards/${cardId}/audit`)).data,
    enabled: cardId !== null,
  })
}

export interface CreateSetPayload {
  id: string
  name: string
  series: string
  printedTotal: number
  total: number
  releaseDate: string
  ptcgoCode?: string
  symbolImageUrl?: string
  logoImageUrl?: string
}

export function useCreateSet() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async (payload: CreateSetPayload) => (await api.post('/admin/sets', payload)).data,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['sets'] }),
  })
}

export interface CreateCardPayload {
  id: string
  number: string
  name: string
  supertype: string
  rarity?: string
  hp?: string
  types?: string[]
  subtypes?: string[]
  artist?: string
  imageSmallUrl?: string
  imageLargeUrl?: string
}

export function useCreateCard(setId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async (payload: CreateCardPayload) => (await api.post(`/admin/sets/${setId}/cards`, payload)).data,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['set-cards-full', setId] })
      queryClient.invalidateQueries({ queryKey: ['card-search'] })
    },
  })
}

// ---- Variant management ----

export function useVariantTypes() {
  return useQuery({
    queryKey: ['admin-variant-types'],
    queryFn: async () => (await api.get<VariantType[]>('/admin/variant-types')).data,
  })
}

export function useCreateVariantType() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async (name: string) => (await api.post<VariantType>('/admin/variant-types', { name })).data,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['admin-variant-types'] }),
  })
}

export function useUpdateVariantType() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async ({ id, name }: { id: string; name: string }) =>
      (await api.put<VariantType>(`/admin/variant-types/${id}`, { name })).data,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['admin-variant-types'] }),
  })
}

export function useDeleteVariantType() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async (id: string) => api.delete(`/admin/variant-types/${id}`),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['admin-variant-types'] }),
  })
}

export function useAddCardVariant() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async ({ cardId, variantTypeId }: { cardId: string; variantTypeId: string }) =>
      api.post(`/admin/cards/${cardId}/variants/${variantTypeId}`),
    onSuccess: (_data, { cardId }) => {
      queryClient.invalidateQueries({ queryKey: ['card-search'] })
      queryClient.invalidateQueries({ queryKey: ['set-cards-full'] })
      queryClient.invalidateQueries({ queryKey: ['card-detail', cardId] })
    },
  })
}

export function useRemoveCardVariant() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async ({ cardId, variantTypeId }: { cardId: string; variantTypeId: string }) =>
      api.delete(`/admin/cards/${cardId}/variants/${variantTypeId}`),
    onSuccess: (_data, { cardId }) => {
      queryClient.invalidateQueries({ queryKey: ['card-search'] })
      queryClient.invalidateQueries({ queryKey: ['set-cards-full'] })
      queryClient.invalidateQueries({ queryKey: ['card-detail', cardId] })
    },
  })
}

export function useBulkAssignVariants() {
  return useMutation({
    mutationFn: async ({
      filters,
      variantTypeIds,
      dryRun,
    }: {
      filters: CardSearchFilters
      variantTypeIds: string[]
      dryRun: boolean
    }) =>
      (
        await api.post<BulkVariantAssignResult>('/admin/variants/bulk-assign', {
          filter: filters,
          variantTypeIds,
          dryRun,
        })
      ).data,
  })
}
