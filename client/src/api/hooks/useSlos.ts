import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import api from '@/api/client'
import type { SloDefinition, CreateSloRequest } from '@/api/types'

export function useSlos(projectId: string | null, probeId: string | null) {
  return useQuery({
    queryKey: ['slos', probeId],
    queryFn: () =>
      api
        .get<SloDefinition[]>(
          `/api/v1/projects/${projectId}/probes/${probeId}/slos`,
        )
        .then(r => r.data),
    enabled: projectId !== null && probeId !== null,
    initialData: [],
  })
}

export function useCreateSlo(projectId: string, probeId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (body: CreateSloRequest) =>
      api
        .post<SloDefinition>(
          `/api/v1/projects/${projectId}/probes/${probeId}/slos`,
          body,
        )
        .then(r => r.data),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['slos', probeId] })
    },
  })
}
