import { useState, useMemo } from 'react'
import { format } from 'date-fns'
import {
  ResponsiveContainer,
  LineChart,
  Line,
  XAxis,
  YAxis,
  Tooltip,
} from 'recharts'
import { Button } from '@/components/ui/button'
import { useProbeChecks } from '@/api/hooks/useHealthChecks'
import { cn } from '@/lib/utils'
import type { HealthCheck } from '@/api/types'

type ChartWindow = '1h' | '24h' | '7d' | '30d'

const DURATIONS_MS: Record<ChartWindow, number> = {
  '1h':  60 * 60 * 1000,
  '24h': 24 * 60 * 60 * 1000,
  '7d':  7 * 24 * 60 * 60 * 1000,
  '30d': 30 * 24 * 60 * 60 * 1000,
}

const BUCKET_MS: Record<ChartWindow, number> = {
  '1h':  5 * 60 * 1000,
  '24h': 30 * 60 * 1000,
  '7d':  6 * 60 * 60 * 1000,
  '30d': 24 * 60 * 60 * 1000,
}

const X_FORMAT: Record<ChartWindow, (ts: number) => string> = {
  '1h':  ts => format(new Date(ts), 'HH:mm'),
  '24h': ts => format(new Date(ts), 'HH:mm'),
  '7d':  ts => format(new Date(ts), 'EEE d'),
  '30d': ts => format(new Date(ts), 'MMM d'),
}

function pct(values: number[], p: number): number {
  const sorted = [...values].sort((a, b) => a - b)
  const idx = Math.ceil((p / 100) * sorted.length) - 1
  return sorted[Math.max(0, idx)] ?? 0
}

interface BucketPoint {
  ts: number
  p50: number
  p95: number
  p99: number
}

function bucket(checks: HealthCheck[], from: Date, bucketMs: number): BucketPoint[] {
  const map = new Map<number, number[]>()
  const origin = from.getTime()
  for (const c of checks) {
    if (!c.isSuccess) continue
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

const LINES = [
  { key: 'p50', stroke: 'hsl(var(--muted-foreground))', opacity: 0.6 },
  { key: 'p95', stroke: '#3b82f6', opacity: 1 },
  { key: 'p99', stroke: '#f59e0b', opacity: 1 },
]

interface ResponseTimeChartProps {
  probeId: string
  projectId: string
}

export function ResponseTimeChart({ probeId, projectId }: ResponseTimeChartProps) {
  const [win, setWin] = useState<ChartWindow>('24h')

  const { data: checks } = useProbeChecks(projectId, probeId, DURATIONS_MS[win])
  const points = useMemo(() => {
    const from = new Date(Date.now() - DURATIONS_MS[win])
    return bucket(checks, from, BUCKET_MS[win])
  }, [checks, win])

  return (
    <div data-testid="response-time-chart" className="space-y-3">
      {/* Header row */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-4">
          <p className="text-[10px] uppercase tracking-widest text-muted-foreground/70">
            Response time
          </p>
          {LINES.map(l => (
            <span key={l.key} className="flex items-center gap-1.5">
              <span
                className="inline-block h-px w-4"
                style={{ background: l.stroke, opacity: l.opacity }}
              />
              <span className="font-mono text-[10px] text-muted-foreground">{l.key}</span>
            </span>
          ))}
        </div>
        <div className="flex gap-1">
          {(['1h', '24h', '7d', '30d'] as ChartWindow[]).map(w => (
            <Button
              key={w}
              variant={win === w ? 'secondary' : 'ghost'}
              size="sm"
              className={cn(
                'h-6 px-2 font-mono text-[10px]',
                win !== w && 'text-muted-foreground hover:text-foreground',
              )}
              onClick={() => setWin(w)}
            >
              {w}
            </Button>
          ))}
        </div>
      </div>

      {/* Chart or empty state */}
      {points.length === 0 ? (
        <div className="flex h-[240px] items-center justify-center rounded-lg border border-dashed border-border">
          <p className="text-xs text-muted-foreground">
            {checks.length > 0
              ? 'All checks failed in this window — probe may be experiencing an outage'
              : 'No check data for this window'}
          </p>
        </div>
      ) : (
        <div className="h-[240px]">
          <ResponsiveContainer width="100%" height="100%">
            <LineChart data={points} margin={{ top: 4, right: 4, bottom: 0, left: 0 }}>
              <XAxis
                dataKey="ts"
                type="number"
                domain={['dataMin', 'dataMax']}
                tickFormatter={(ts: number) => X_FORMAT[win](ts)}
                tick={{ fontSize: 10, fontFamily: 'monospace', fill: 'hsl(var(--muted-foreground))' }}
                tickLine={false}
                axisLine={false}
                scale="time"
              />
              <YAxis
                tickFormatter={(v: number) => `${v}ms`}
                tick={{ fontSize: 10, fontFamily: 'monospace', fill: 'hsl(var(--muted-foreground))' }}
                tickLine={false}
                axisLine={false}
                width={52}
              />
              <Tooltip
                labelFormatter={(label) => {
                  const ts = Number(label)
                  return isNaN(ts) ? String(label) : format(new Date(ts), 'MMM d, HH:mm')
                }}
                formatter={(value, name) => [`${value ?? '—'}ms`, String(name)]}
                contentStyle={{
                  background: 'hsl(var(--card))',
                  border: '1px solid hsl(var(--border))',
                  borderRadius: '6px',
                  fontSize: '11px',
                  fontFamily: 'monospace',
                  padding: '8px 12px',
                  boxShadow: '0 4px 16px rgba(0,0,0,0.3)',
                }}
                itemStyle={{ color: 'hsl(var(--foreground))' }}
                labelStyle={{ color: 'hsl(var(--muted-foreground))', marginBottom: '4px' }}
                cursor={{ strokeDasharray: '3 3', stroke: 'hsl(var(--border))' }}
              />
              {LINES.map(l => (
                <Line
                  key={l.key}
                  type="monotone"
                  dataKey={l.key}
                  name={l.key}
                  stroke={l.stroke}
                  strokeWidth={1.5}
                  strokeOpacity={l.opacity}
                  dot={false}
                  activeDot={{ r: 3, strokeWidth: 0 }}
                />
              ))}
            </LineChart>
          </ResponsiveContainer>
        </div>
      )}
    </div>
  )
}
