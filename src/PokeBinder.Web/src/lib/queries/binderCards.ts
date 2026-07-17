import { useQuery } from '@tanstack/react-query'
import { api } from '../api'
import type { BinderCardRow } from '../binder-cards-types'

export function useBinderCards(binderId: string) {
  return useQuery({
    queryKey: ['binder-cards', binderId],
    queryFn: async () => (await api.get<BinderCardRow[]>(`/binders/${binderId}/cards`)).data,
  })
}
