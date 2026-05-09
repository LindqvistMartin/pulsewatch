import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import api from '@/api/client'
import type { Org, CreateOrgRequest } from '@/api/types'

export function useOrganizations() {
  return useQuery({
    queryKey: ['orgs'],
    queryFn: () => api.get<Org[]>('/api/v1/organizations').then(r => r.data),
    initialData: [],
  })
}

export function useCreateOrganization() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (body: CreateOrgRequest) =>
      api.post<Org>('/api/v1/organizations', body).then(r => r.data),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['orgs'] })
    },
  })
}
