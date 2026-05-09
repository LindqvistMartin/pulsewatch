import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import api from '@/api/client'
import type { Probe, CreateProbeRequest } from '@/api/types'

export function useProbes(projectId: string | null) {
  return useQuery({
    queryKey: ['probes', projectId],
    queryFn: () =>
      api.get<Probe[]>(`/api/v1/projects/${projectId}/probes`).then(r => r.data),
    enabled: projectId !== null,
    initialData: [],
  })
}

export function useProbe(projectId: string | null, probeId: string | null) {
  return useQuery({
    queryKey: ['probe', probeId],
    queryFn: () =>
      api
        .get<Probe>(`/api/v1/projects/${projectId}/probes/${probeId}`)
        .then(r => r.data),
    enabled: projectId !== null && probeId !== null,
  })
}

export function useCreateProbe(projectId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (body: CreateProbeRequest) =>
      api
        .post<Probe>(`/api/v1/projects/${projectId}/probes`, body)
        .then(r => r.data),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['probes', projectId] })
    },
  })
}

export function useDeleteProbe(projectId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (probeId: string) =>
      api.delete(`/api/v1/projects/${projectId}/probes/${probeId}`),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['probes', projectId] })
    },
  })
}
