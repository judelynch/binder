import { useMutation, useQueryClient, type QueryClient } from '@tanstack/react-query'
import { api } from '../api'

export interface OwnershipResult {
  cardVariantId: string
  owned: boolean
  quantity: number
  condition: string | null
}

/**
 * Ownership changes ripple across three surfaces (the Sets grid's completion badges, a set's
 * card list, and a card's own detail page) that any given toggle click can't know it's not on -
 * invalidate all three broadly rather than plumbing setId/cardId through every call site. The
 * catalog-sized query results here are cheap to refetch.
 */
function invalidateOwnershipQueries(queryClient: QueryClient) {
  queryClient.invalidateQueries({ queryKey: ['sets'] })
  queryClient.invalidateQueries({ queryKey: ['set-cards-full'] })
  queryClient.invalidateQueries({ queryKey: ['card-detail'] })
}

export function useSetOwnership() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async ({
      cardVariantId,
      quantity,
      condition,
    }: {
      cardVariantId: string
      quantity: number
      condition?: string | null
    }) =>
      (await api.put<OwnershipResult>(`/collection/ownership/${cardVariantId}`, { quantity, condition: condition ?? null })).data,
    onSuccess: () => invalidateOwnershipQueries(queryClient),
  })
}

export function useUnsetOwnership() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async (cardVariantId: string) => {
      await api.delete(`/collection/ownership/${cardVariantId}`)
    },
    onSuccess: () => invalidateOwnershipQueries(queryClient),
  })
}

export interface BulkOwnershipResult {
  count: number
}

/** Select-then-bulk-mark flow (the set-detail page's "select all" + "mark as owned/not owned"). */
export function useBulkSetOwnership() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async ({ cardVariantIds, owned }: { cardVariantIds: string[]; owned: boolean }) =>
      (await api.post<BulkOwnershipResult>('/collection/ownership/bulk', { cardVariantIds, owned })).data,
    onSuccess: () => invalidateOwnershipQueries(queryClient),
  })
}
