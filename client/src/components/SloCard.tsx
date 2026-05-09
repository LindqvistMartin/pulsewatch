import { formatDistanceToNow } from 'date-fns'
import { Card, CardContent, CardHeader } from '@/components/ui/card'
import { cn } from '@/lib/utils'
import type { SloDefinition } from '@/api/types'

interface SloCardProps {
  slo: SloDefinition
}

export function SloCard({ slo }: SloCardProps) {
  const m = slo.latestMeasurement

  if (!m) {
    return (
      <Card className="border-border">
        <CardHeader className="pb-3">
          <p className="text-[10px] uppercase tracking-widest text-muted-foreground/70">SLO</p>
        </CardHeader>
        <CardContent>
          <p className="text-xs text-muted-foreground">
            No measurements yet — SLO calculator runs every 60s.
          </p>
        </CardContent>
      </Card>
    )
  }

  const budgetConsumedPct =
    m.errorBudgetTotalSeconds > 0
      ? Math.min(100, (m.errorBudgetConsumedSeconds / m.errorBudgetTotalSeconds) * 100)
      : 0

  const availClass =
    m.availabilityPct >= slo.targetAvailabilityPct
      ? 'text-emerald-600 dark:text-emerald-400'
      : 'text-red-500 dark:text-red-400'

  const burnRateClass =
    m.burnRate <= 1
      ? 'text-emerald-600 dark:text-emerald-400'
      : m.burnRate <= 2
        ? 'text-amber-500 dark:text-amber-400'
        : 'text-red-500 dark:text-red-400'

  return (
    <Card className="border-border">
      <CardHeader className="pb-3">
        <div className="flex items-center justify-between">
          <p className="text-[10px] uppercase tracking-widest text-muted-foreground/70">SLO</p>
          <span className="font-mono text-[10px] text-muted-foreground">
            {slo.targetAvailabilityPct}% / {slo.windowDays}d
          </span>
        </div>
      </CardHeader>
      <CardContent className="space-y-5">
        {/* Availability */}
        <div>
          <p className="mb-1 text-[10px] uppercase tracking-widest text-muted-foreground/60">
            Availability
          </p>
          <p className={cn('font-mono text-2xl font-semibold tabular-nums leading-none', availClass)}>
            {m.availabilityPct.toFixed(3)}%
          </p>
          <p className="mt-0.5 font-mono text-[10px] text-muted-foreground/60">
            target {slo.targetAvailabilityPct}%
          </p>
        </div>

        {/* Error budget progress */}
        <div>
          <div className="mb-1.5 flex items-center justify-between">
            <p className="text-[10px] uppercase tracking-widest text-muted-foreground/60">
              Error budget
            </p>
            <span className="font-mono text-[10px] text-muted-foreground tabular-nums">
              {budgetConsumedPct.toFixed(1)}% consumed
            </span>
          </div>
          <div className="h-1 w-full overflow-hidden rounded-full bg-muted">
            <div
              className="h-full rounded-full bg-red-500 transition-all duration-500 ease-out"
              style={{ width: `${budgetConsumedPct}%` }}
            />
          </div>
        </div>

        {/* Burn rate + exhaustion */}
        <div className="flex items-start justify-between">
          <div>
            <p className="mb-1 text-[10px] uppercase tracking-widest text-muted-foreground/60">
              Burn rate
            </p>
            <p className={cn('font-mono text-xl font-semibold tabular-nums leading-none', burnRateClass)}>
              {m.burnRate.toFixed(2)}×
            </p>
          </div>
          <div className="text-right">
            <p className="mb-1 text-[10px] uppercase tracking-widest text-muted-foreground/60">
              Exhaustion
            </p>
            <p className="font-mono text-xs text-muted-foreground">
              {m.projectedExhaustionAt && new Date(m.projectedExhaustionAt) > new Date()
                ? `in ${formatDistanceToNow(new Date(m.projectedExhaustionAt), { addSuffix: false })}`
                : m.burnRate > 1
                  ? 'Budget exhausted'
                  : 'Budget healthy'}
            </p>
          </div>
        </div>
      </CardContent>
    </Card>
  )
}
