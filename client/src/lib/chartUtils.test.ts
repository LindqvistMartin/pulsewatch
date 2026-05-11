import { describe, it, expect } from 'vitest'
import type { HealthCheck } from '@/api/types'
import { bucketChecks, pct } from './chartUtils'

const FROM = new Date('2024-01-01T00:00:00Z')
const BUCKET_30M = 30 * 60 * 1000

function makeCheck(overrides: Partial<HealthCheck> = {}): HealthCheck {
  return {
    id: 'c1',
    statusCode: 200,
    responseTimeMs: 100,
    isSuccess: true,
    failureReason: null,
    checkedAt: '2024-01-01T00:05:00Z',
    ...overrides,
  }
}

describe('pct', () => {
  it('returns the correct percentile', () => {
    expect(pct([1, 2, 3, 4, 5], 50)).toBe(3)
  })

  it('handles a single value', () => {
    expect(pct([42], 95)).toBe(42)
  })
})

describe('bucketChecks', () => {
  it('includes failed checks that have a measured response time', () => {
    // This was the bug: HTTP 500 checks were skipped, leaving the chart empty.
    const checks = [
      makeCheck({ isSuccess: false, statusCode: 500, responseTimeMs: 64 }),
      makeCheck({ isSuccess: false, statusCode: 500, responseTimeMs: 65 }),
    ]
    const points = bucketChecks(checks, FROM, BUCKET_30M)
    expect(points).toHaveLength(1)
    expect(points[0].p50).toBe(64)
  })

  it('excludes checks with no measured response time', () => {
    const checks = [
      makeCheck({ responseTimeMs: null as unknown as number }),
    ]
    const points = bucketChecks(checks, FROM, BUCKET_30M)
    expect(points).toHaveLength(0)
  })

  it('groups checks into buckets by time', () => {
    const checks = [
      makeCheck({ responseTimeMs: 100, checkedAt: '2024-01-01T00:05:00Z' }),
      makeCheck({ responseTimeMs: 200, checkedAt: '2024-01-01T00:20:00Z' }),
      makeCheck({ responseTimeMs: 300, checkedAt: '2024-01-01T00:35:00Z' }),
    ]
    const points = bucketChecks(checks, FROM, BUCKET_30M)
    // first two fall in bucket [0:00, 0:30), third in [0:30, 1:00)
    expect(points).toHaveLength(2)
    expect(points[0].p50).toBe(100) // nearest-rank p50 of [100, 200]
    expect(points[1].p50).toBe(300)
  })

  it('returns empty array when checks list is empty', () => {
    expect(bucketChecks([], FROM, BUCKET_30M)).toHaveLength(0)
  })
})
