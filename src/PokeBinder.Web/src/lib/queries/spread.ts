import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '../api'
import type { BinderSlot, SlotCondition, Spread } from '../spread-types'
import { bindersKey, dashboardKey } from './binders'

export function spreadKey(binderId: string, spreadIndex: number) {
  return ['spread', binderId, spreadIndex] as const
}

export function useSpread(binderId: string, spreadIndex: number) {
  return useQuery({
    queryKey: spreadKey(binderId, spreadIndex),
    queryFn: async () => (await api.get<Spread>(`/binders/${binderId}/spread/${spreadIndex}`)).data,
    enabled: spreadIndex >= 0,
  })
}

function replaceSlotInSpread(spread: Spread, updated: BinderSlot): Spread {
  const patchPanel = (panel: Spread['leftPanel']) =>
    panel.slots
      ? { ...panel, slots: panel.slots.map((s) => (s.slotId === updated.slotId ? updated : s)) }
      : panel

  return { ...spread, leftPanel: patchPanel(spread.leftPanel), rightPanel: patchPanel(spread.rightPanel) }
}

function invalidateSpreadAndSummaries(queryClient: ReturnType<typeof useQueryClient>, binderId: string) {
  queryClient.invalidateQueries({ queryKey: ['spread', binderId] })
  queryClient.invalidateQueries({ queryKey: bindersKey })
  queryClient.invalidateQueries({ queryKey: dashboardKey })
}

export function useAssignCard(binderId: string, spreadIndex: number) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async ({ slotId, cardVariantId }: { slotId: string; cardVariantId: string }) =>
      (await api.put<BinderSlot>(`/binders/${binderId}/slots/${slotId}`, { cardVariantId })).data,
    onSuccess: (updated) => {
      queryClient.setQueryData<Spread>(spreadKey(binderId, spreadIndex), (old) =>
        old ? replaceSlotInSpread(old, updated) : old,
      )
      invalidateSpreadAndSummaries(queryClient, binderId)
    },
  })
}

export function useUnassignSlot(binderId: string, spreadIndex: number) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async (slotId: string) => (await api.delete<BinderSlot>(`/binders/${binderId}/slots/${slotId}`)).data,
    onSuccess: (updated) => {
      queryClient.setQueryData<Spread>(spreadKey(binderId, spreadIndex), (old) =>
        old ? replaceSlotInSpread(old, updated) : old,
      )
      invalidateSpreadAndSummaries(queryClient, binderId)
    },
  })
}

export function useUpdateSlotState(binderId: string, spreadIndex: number) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async ({
      slotId,
      ...body
    }: {
      slotId: string
      owned?: boolean
      quantity?: number
      condition?: SlotCondition
    }) => (await api.patch<BinderSlot>(`/binders/${binderId}/slots/${slotId}`, body)).data,
    onSuccess: (updated) => {
      queryClient.setQueryData<Spread>(spreadKey(binderId, spreadIndex), (old) =>
        old ? replaceSlotInSpread(old, updated) : old,
      )
      invalidateSpreadAndSummaries(queryClient, binderId)
    },
  })
}

export function useSetOverlayTag(binderId: string, spreadIndex: number) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async ({ slotId, overlayTagId }: { slotId: string; overlayTagId: string | null }) =>
      (await api.patch<BinderSlot>(`/binders/${binderId}/slots/${slotId}/overlay-tag`, { overlayTagId })).data,
    onSuccess: (updated) => {
      queryClient.setQueryData<Spread>(spreadKey(binderId, spreadIndex), (old) =>
        old ? replaceSlotInSpread(old, updated) : old,
      )
    },
  })
}

export function useMoveSlot(binderId: string, spreadIndex: number) {
  const queryClient = useQueryClient()
  const key = spreadKey(binderId, spreadIndex)

  return useMutation({
    mutationFn: async ({ slotId, targetSlotId }: { slotId: string; targetSlotId: string }) =>
      (await api.post<{ source: BinderSlot; target: BinderSlot }>(`/binders/${binderId}/slots/${slotId}/move`, {
        targetSlotId,
      })).data,
    onMutate: async ({ slotId, targetSlotId }) => {
      await queryClient.cancelQueries({ queryKey: key })
      const previous = queryClient.getQueryData<Spread>(key)

      queryClient.setQueryData<Spread>(key, (old) => {
        if (!old) return old
        const findSlot = (s: Spread) =>
          [...(s.leftPanel.slots ?? []), ...(s.rightPanel.slots ?? [])].reduce<Record<string, BinderSlot>>(
            (acc, slot) => ({ ...acc, [slot.slotId]: slot }),
            {},
          )
        const bySlotId = findSlot(old)
        const source = bySlotId[slotId]
        const target = bySlotId[targetSlotId]
        if (!source || !target) return old

        const swappedSource: BinderSlot = {
          ...source,
          card: target.card,
          variantTypeName: target.variantTypeName,
          owned: target.owned,
          quantity: target.quantity,
          condition: target.condition,
          overlayTag: target.overlayTag,
        }
        const swappedTarget: BinderSlot = {
          ...target,
          card: source.card,
          variantTypeName: source.variantTypeName,
          owned: source.owned,
          quantity: source.quantity,
          condition: source.condition,
          overlayTag: source.overlayTag,
        }

        let next = replaceSlotInSpread(old, swappedSource)
        next = replaceSlotInSpread(next, swappedTarget)
        return next
      })

      return { previous }
    },
    onError: (_err, _vars, context) => {
      if (context?.previous) {
        queryClient.setQueryData(key, context.previous)
      }
    },
    onSuccess: ({ source, target }) => {
      queryClient.setQueryData<Spread>(key, (old) => {
        if (!old) return old
        let next = replaceSlotInSpread(old, source)
        next = replaceSlotInSpread(next, target)
        return next
      })
      invalidateSpreadAndSummaries(queryClient, binderId)
    },
  })
}

export function useAppendPages(binderId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async (count: number) => (await api.post(`/binders/${binderId}/pages`, { count })).data,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['spread', binderId] })
      queryClient.invalidateQueries({ queryKey: bindersKey })
    },
  })
}
