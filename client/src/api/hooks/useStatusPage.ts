import { useQuery } from '@tanstack/react-query'
import api from '@/api/client'
import type { StatusPageSnapshot } from '@/api/types'

export function useStatusPage(slug: string) {
  return useQuery({
    queryKey: ['status-page', slug],
    queryFn: () =>
      api.get<StatusPageSnapshot>(`/public/status/${slug}`).then((r) => r.data),
    enabled: !!slug,
    retry: false,
  })
}
