import type { HealthCheck } from '@/api/types'

export interface BucketPoint {
  ts: number
  p50: number
  p95: number
  p99: number
}

export function pct(values: number[], p: number): number {
  const sorted = [...values].sort((a, b) => a - b)
  const idx = Math.ceil((p / 100) * sorted.length) - 1
  return sorted[Math.max(0, idx)] ?? 0
}

export function bucketChecks(
  checks: HealthCheck[],
  from: Date,
  bucketMs: number,
): BucketPoint[] {
  const map = new Map<number, number[]>()
  const origin = from.getTime()
  for (const c of checks) {
    // exclude only checks where no response time was measured (e.g. DNS failure before connection)
    if (c.responseTimeMs == null) continue
    const t = new Date(c.checkedAt).getTime()
    const key = Math.floor((t - origin) / bucketMs) * bucketMs + origin
    const arr = map.get(key) ?? []
    arr.push(c.responseTimeMs)
    map.set(key, arr)
  }
  return Array.from(map.entries())
    .sort(([a], [b]) => a - b)
    .map(([ts, vals]) => ({ ts, p50: pct(vals, 50), p95: pct(vals, 95), p99: pct(vals, 99) }))
}
