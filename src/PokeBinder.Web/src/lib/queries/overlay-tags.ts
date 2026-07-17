import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '../api'
import type { OverlayTag } from '../spread-types'

export const overlayTagsKey = ['overlay-tags'] as const

export function useOverlayTags() {
  return useQuery({
    queryKey: overlayTagsKey,
    queryFn: async () => (await api.get<OverlayTag[]>('/overlay-tags')).data,
  })
}

export function useCreateOverlayTag() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async (input: { name: string; colourHex: string }) =>
      (await api.post<OverlayTag>('/overlay-tags', input)).data,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: overlayTagsKey })
    },
  })
}

export function useUpdateOverlayTag() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async ({ id, ...input }: { id: string; name?: string; colourHex?: string }) =>
      (await api.patch<OverlayTag>(`/overlay-tags/${id}`, input)).data,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: overlayTagsKey })
    },
  })
}

/** Deleting a tag definition also un-assigns it from every slot that had it (server-side SetNull),
 * so any currently-open binder spread needs to refetch too - invalidate broadly rather than
 * plumbing a binderId through, since this hook has no reason to otherwise know which binder(s)
 * are affected. */
export function useDeleteOverlayTag() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async (id: string) => {
      await api.delete(`/overlay-tags/${id}`)
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: overlayTagsKey })
      queryClient.invalidateQueries({ queryKey: ['spread'] })
    },
  })
}
