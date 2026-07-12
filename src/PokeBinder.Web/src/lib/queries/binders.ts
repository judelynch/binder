import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '../api'
import type { BinderSummary, CreateBinderInput, Dashboard, UpdateBinderInput } from '../binder-types'

export const bindersKey = ['binders'] as const
export const dashboardKey = ['dashboard'] as const

export function useBinders() {
  return useQuery({
    queryKey: bindersKey,
    queryFn: async () => (await api.get<BinderSummary[]>('/binders')).data,
  })
}

export function useBinder(id: string) {
  return useQuery({
    queryKey: [...bindersKey, id],
    queryFn: async () => (await api.get<BinderSummary>(`/binders/${id}`)).data,
  })
}

export function useDashboard() {
  return useQuery({
    queryKey: dashboardKey,
    queryFn: async () => (await api.get<Dashboard>('/dashboard')).data,
  })
}

export function useCreateBinder() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async (input: CreateBinderInput) => (await api.post<BinderSummary>('/binders', input)).data,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: bindersKey })
      queryClient.invalidateQueries({ queryKey: dashboardKey })
    },
  })
}

export function useUpdateBinder(id: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async (input: UpdateBinderInput) => (await api.patch<BinderSummary>(`/binders/${id}`, input)).data,
    onMutate: async (input) => {
      await queryClient.cancelQueries({ queryKey: bindersKey })
      const previous = queryClient.getQueryData<BinderSummary[]>(bindersKey)

      queryClient.setQueryData<BinderSummary[]>(bindersKey, (old) =>
        old?.map((binder) => (binder.id === id ? { ...binder, ...input } : binder)),
      )

      return { previous }
    },
    onError: (_err, _input, context) => {
      if (context?.previous) {
        queryClient.setQueryData(bindersKey, context.previous)
      }
    },
    onSettled: () => {
      queryClient.invalidateQueries({ queryKey: bindersKey })
    },
  })
}

export function useDeleteBinder() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async (id: string) => {
      await api.delete(`/binders/${id}`)
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: bindersKey })
      queryClient.invalidateQueries({ queryKey: dashboardKey })
    },
  })
}
