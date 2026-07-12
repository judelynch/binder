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
