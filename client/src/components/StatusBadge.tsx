import { cva } from 'class-variance-authority'
import { cn } from '@/lib/utils'

export type ProbeStatus = 'healthy' | 'degraded' | 'down' | 'unknown'

const dotClass = cva('h-1.5 w-1.5 rounded-full', {
  variants: {
    status: {
      healthy: 'bg-emerald-500',
      degraded: 'bg-amber-400',
      down: 'bg-red-500',
      unknown: 'bg-muted-foreground/40',
    },
  },
})

const labelClass = cva('font-mono text-xs', {
  variants: {
    status: {
      healthy: 'text-emerald-600 dark:text-emerald-400',
      degraded: 'text-amber-600 dark:text-amber-400',
      down: 'text-red-500 dark:text-red-400',
      unknown: 'text-muted-foreground',
    },
  },
})

const LABELS: Record<ProbeStatus, string> = {
  healthy: 'Healthy',
  degraded: 'Degraded',
  down: 'Down',
  unknown: 'Unknown',
}

interface StatusBadgeProps {
  status: ProbeStatus
  animated?: boolean
  className?: string
}

export function StatusBadge({ status, animated = false, className }: StatusBadgeProps) {
  return (
    <span className={cn('inline-flex items-center gap-1.5', className)}>
      <span className="relative inline-flex h-1.5 w-1.5 shrink-0">
        <span className={dotClass({ status })} />
        {animated && (status === 'healthy' || status === 'down') && (
          <span
            className={cn(
              dotClass({ status }),
              'absolute inset-0 animate-ping opacity-60',
            )}
          />
        )}
      </span>
      <span className={labelClass({ status })}>{LABELS[status]}</span>
    </span>
  )
}
