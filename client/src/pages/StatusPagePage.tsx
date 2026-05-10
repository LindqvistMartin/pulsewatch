import { useState } from 'react'
import { useParams, Link } from 'react-router-dom'
import { CheckCircle2, AlertTriangle, XCircle, AlertCircle } from 'lucide-react'
import { useStatusPage } from '@/api/hooks/useStatusPage'
import { StatusBadge } from '@/components/StatusBadge'
import type { DailyBar, ProbeSnapshot } from '@/api/types'

function relativeTime(iso: string): string {
  const diff = Date.now() - new Date(iso).getTime()
  const mins = Math.floor(diff / 60_000)
  if (mins < 60) return `${mins}m ago`
  const hours = Math.floor(mins / 60)
  if (hours < 24) return `${hours}h ago`
  return `${Math.floor(hours / 24)}d ago`
}

function barColor(bar: DailyBar): string {
  if (bar.totalChecks === 0) return 'bg-muted/20'
  if (bar.availabilityPct >= 99.9) return 'bg-emerald-500/70'
  if (bar.availabilityPct >= 95) return 'bg-amber-500/70'
  return 'bg-red-500/70'
}

function BarTooltip({ bar }: { bar: DailyBar }) {
  const [show, setShow] = useState(false)
  return (
    <div
      className="relative"
      onMouseEnter={() => setShow(true)}
      onMouseLeave={() => setShow(false)}
    >
      <div
        className={`w-[4px] h-7 rounded-sm cursor-default transition-opacity hover:opacity-100 ${barColor(bar)} ${show ? 'opacity-100' : 'opacity-80'}`}
      />
      {show && (
        <div className="absolute bottom-full mb-1.5 left-1/2 -translate-x-1/2 z-10 pointer-events-none">
          <div className="bg-popover border border-border rounded-md px-2 py-1 text-[10px] font-mono text-popover-foreground whitespace-nowrap shadow-md">
            <div className="text-muted-foreground">{bar.date}</div>
            <div className="text-foreground font-medium">
              {bar.totalChecks === 0 ? 'No data' : `${bar.availabilityPct.toFixed(2)}%`}
            </div>
          </div>
        </div>
      )}
    </div>
  )
}

function HistoricalBars({ bars }: { bars: DailyBar[] }) {
  return (
    <div className="flex items-end gap-px overflow-hidden">
      {bars.map((bar, i) => (
        <BarTooltip key={i} bar={bar} />
      ))}
    </div>
  )
}

function ProbeRow({ probe }: { probe: ProbeSnapshot }) {
  const status = probe.status === 'Healthy' ? 'healthy'
    : probe.status === 'Down' ? 'down'
    : 'degraded'

  return (
    <div className="py-4 border-b border-border/50 last:border-0">
      <div className="flex items-center justify-between mb-2.5">
        <div className="flex items-center gap-2.5">
          <StatusBadge status={status} />
          <span className="text-sm font-medium text-foreground">{probe.name}</span>
        </div>
        <span className="text-[10px] font-mono text-muted-foreground">90 days</span>
      </div>
      <HistoricalBars bars={probe.dailyBars} />
    </div>
  )
}

function SkeletonRow() {
  return (
    <div className="py-4 border-b border-border/50 last:border-0 animate-pulse">
      <div className="flex items-center gap-2.5 mb-2.5">
        <div className="h-5 w-14 rounded-full bg-muted/30" />
        <div className="h-4 w-32 rounded bg-muted/30" />
      </div>
      <div className="flex items-end gap-px">
        {Array.from({ length: 90 }).map((_, i) => (
          <div key={i} className="w-[4px] h-7 rounded-sm bg-muted/20" />
        ))}
      </div>
    </div>
  )
}

const statusConfig = {
  Operational: {
    icon: CheckCircle2,
    text: 'All systems operational',
    bg: 'bg-emerald-500/10 border-emerald-500/20',
    iconColor: 'text-emerald-500',
    textColor: 'text-emerald-400',
  },
  Degraded: {
    icon: AlertTriangle,
    text: 'Partial outage detected',
    bg: 'bg-amber-500/10 border-amber-500/20',
    iconColor: 'text-amber-500',
    textColor: 'text-amber-400',
  },
  Outage: {
    icon: XCircle,
    text: 'Major outage',
    bg: 'bg-red-500/10 border-red-500/20',
    iconColor: 'text-red-500',
    textColor: 'text-red-400',
  },
} as const

export function StatusPagePage() {
  const { slug } = useParams<{ slug: string }>()
  const { data, isLoading, isError } = useStatusPage(slug ?? '')

  if (isError) {
    return (
      <div className="min-h-screen flex flex-col items-center justify-center bg-background text-center px-4">
        <AlertCircle className="h-10 w-10 text-muted-foreground/40 mb-4" />
        <h1 className="text-lg font-semibold text-foreground">Status page not found</h1>
        <p className="mt-1 text-sm text-muted-foreground">
          The status page <span className="font-mono">{slug}</span> does not exist.
        </p>
        <Link
          to="/dashboard"
          className="mt-6 text-xs font-mono text-muted-foreground underline underline-offset-4 hover:text-foreground transition-colors"
        >
          ← Back to dashboard
        </Link>
      </div>
    )
  }

  const cfg = data
    ? (statusConfig[data.overallStatus as keyof typeof statusConfig] ?? statusConfig.Degraded)
    : null

  return (
    <div className="min-h-screen bg-background">
      {/* Subtle grid background */}
      <div
        className="fixed inset-0 pointer-events-none opacity-[0.03]"
        style={{
          backgroundImage: 'radial-gradient(circle, hsl(var(--foreground)) 1px, transparent 1px)',
          backgroundSize: '24px 24px',
        }}
      />

      <div className="relative mx-auto max-w-3xl px-4 py-16">
        {/* Header */}
        <div className="mb-10">
          {isLoading ? (
            <div className="animate-pulse space-y-2">
              <div className="h-7 w-48 rounded bg-muted/30" />
              <div className="h-4 w-72 rounded bg-muted/20" />
            </div>
          ) : (
            <>
              <h1 className="text-xl font-semibold tracking-tight text-foreground">
                {data?.title}
              </h1>
              {data?.description && (
                <p className="mt-1 text-sm text-muted-foreground">{data.description}</p>
              )}
            </>
          )}
        </div>

        {/* Status banner */}
        {isLoading ? (
          <div className="animate-pulse h-14 rounded-lg bg-muted/20 border border-border mb-8" />
        ) : cfg ? (
          <div
            className={`flex items-center gap-3 rounded-lg border px-5 py-3.5 mb-8 ${cfg.bg}`}
            data-testid="status-banner"
          >
            <cfg.icon className={`h-5 w-5 shrink-0 ${cfg.iconColor}`} />
            <span className={`text-sm font-medium ${cfg.textColor}`}>{cfg.text}</span>
          </div>
        ) : null}

        {/* Probes */}
        <div className="rounded-lg border border-border bg-card/30 px-5 mb-6">
          {isLoading ? (
            <>
              <SkeletonRow />
              <SkeletonRow />
              <SkeletonRow />
            </>
          ) : (
            data?.probes.map((probe) => <ProbeRow key={probe.id} probe={probe} />)
          )}
        </div>

        {/* Active incidents */}
        {!isLoading && data && data.activeIncidents.length > 0 && (
          <div className="rounded-lg border border-amber-500/20 bg-amber-500/5 px-5 py-4 mb-6">
            <h2 className="text-xs font-mono font-semibold text-amber-400 uppercase tracking-wider mb-3">
              Active incidents
            </h2>
            <div className="space-y-3">
              {data.activeIncidents.map((incident) => (
                <div key={incident.id} className="flex items-start gap-3">
                  <AlertTriangle className="h-3.5 w-3.5 text-amber-500 mt-0.5 shrink-0" />
                  <div>
                    <p className="text-sm text-foreground">{incident.reason}</p>
                    <p className="text-[11px] font-mono text-muted-foreground mt-0.5">
                      Opened {relativeTime(incident.openedAt)}
                    </p>
                  </div>
                </div>
              ))}
            </div>
          </div>
        )}

        {/* Footer */}
        <div className="mt-12 text-center">
          <p className="text-[11px] font-mono text-muted-foreground/50">
            Powered by{' '}
            <Link
              to="/"
              className="hover:text-muted-foreground transition-colors underline-offset-2 hover:underline"
            >
              PulseWatch
            </Link>
          </p>
        </div>
      </div>
    </div>
  )
}
