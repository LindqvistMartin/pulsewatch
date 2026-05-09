import { useQuery } from '@tanstack/react-query'
import api from '@/api/client'
import type { HealthCheck } from '@/api/types'

// windowMs: how far back to fetch (e.g. 24 * 60 * 60 * 1000 for 24h).
// Dates are computed fresh inside queryFn so SignalR-triggered refetches
// always cover "now - windowMs" → "now", not a frozen mount-time range.
export function useProbeChecks(
  projectId: string | null,
  probeId: string | null,
  windowMs: number,
) {
  return useQuery({
    queryKey: ['checks', probeId, windowMs],
    queryFn: () => {
      const to = new Date()
      const from = new Date(to.getTime() - windowMs)
      return api
        .get<HealthCheck[]>(`/api/v1/projects/${projectId}/probes/${probeId}/checks`, {
          params: { from: from.toISOString(), to: to.toISOString() },
        })
        .then(r => r.data)
    },
    enabled: projectId !== null && probeId !== null,
    initialData: [],
  })
}
