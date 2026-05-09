import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import api from '@/api/client'
import type { Project, CreateProjectRequest } from '@/api/types'

export function useProjects(orgId: string | null) {
  return useQuery({
    queryKey: ['projects', orgId],
    queryFn: () =>
      api.get<Project[]>(`/api/v1/organizations/${orgId}/projects`).then(r => r.data),
    enabled: orgId !== null,
    initialData: [],
    initialDataUpdatedAt: 0,
  })
}

export function useCreateProject(orgId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (body: CreateProjectRequest) =>
      api
        .post<Project>(`/api/v1/organizations/${orgId}/projects`, body)
        .then(r => r.data),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['projects', orgId] })
    },
  })
}
