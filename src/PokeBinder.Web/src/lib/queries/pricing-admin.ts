import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '../api'
import type { BulkClassificationActionResult, QueuedListing, ReclassifyPayload, ScrapeRun } from '../admin-types'
import type { PagedResult } from './cards'

function invalidatePricingAdmin(queryClient: ReturnType<typeof useQueryClient>) {
  queryClient.invalidateQueries({ queryKey: ['pricing-queue'] })
}

export function usePricingQueue(page: number, pageSize = 20) {
  return useQuery({
    queryKey: ['pricing-queue', page, pageSize],
    queryFn: async () => (await api.get<PagedResult<QueuedListing>>('/admin/pricing/queue', { params: { page, pageSize } })).data,
  })
}

export function useApproveListing() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async (classificationId: string) => api.post(`/admin/pricing/queue/${classificationId}/approve`, {}),
    onSuccess: () => invalidatePricingAdmin(queryClient),
  })
}

export function useReclassifyListing() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async ({ classificationId, payload }: { classificationId: string; payload: ReclassifyPayload }) =>
      api.post(`/admin/pricing/queue/${classificationId}/reclassify`, payload),
    onSuccess: () => invalidatePricingAdmin(queryClient),
  })
}

export function useRejectListing() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async ({ classificationId, reason }: { classificationId: string; reason?: string }) =>
      api.post(`/admin/pricing/queue/${classificationId}/reject`, { reason }),
    onSuccess: () => invalidatePricingAdmin(queryClient),
  })
}

export function useBulkClassificationAction() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async ({
      classificationIds,
      action,
      reason,
    }: {
      classificationIds: string[]
      action: 'Approve' | 'Reject'
      reason?: string
    }) =>
      (await api.post<BulkClassificationActionResult>('/admin/pricing/queue/bulk', { classificationIds, action, reason })).data,
    onSuccess: () => invalidatePricingAdmin(queryClient),
  })
}

export function usePricingRunHistory(page: number, pageSize = 20) {
  return useQuery({
    queryKey: ['pricing-runs', page, pageSize],
    queryFn: async () => (await api.get<PagedResult<ScrapeRun>>('/admin/pricing/runs', { params: { page, pageSize } })).data,
    refetchInterval: (query) => (query.state.data?.items.some((r) => r.status === 'Running') ? 2000 : false),
  })
}

export function useRunPricingNow() {
  return useMutation({
    mutationFn: async () => api.post('/admin/pricing/run'),
  })
}
