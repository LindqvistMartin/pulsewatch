import { useQuery } from '@tanstack/react-query'
import api from '@/api/client'
import type { Incident } from '@/api/types'

export function useIncidents(projectId: string | null, probeId: string | null) {
  return useQuery({
    queryKey: ['incidents', probeId],
    queryFn: () =>
      api
        .get<Incident[]>(
          `/api/v1/projects/${projectId}/probes/${probeId}/incidents`,
        )
        .then(r => r.data),
    enabled: projectId !== null && probeId !== null,
    initialData: [],
    initialDataUpdatedAt: 0,
  })
}
