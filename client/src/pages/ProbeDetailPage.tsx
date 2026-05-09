import { useState, useMemo } from 'react'
import { Link, useParams, useNavigate } from 'react-router-dom'
import { ArrowLeft, Trash2 } from 'lucide-react'
import { formatDistanceToNow } from 'date-fns'
import { Card, CardContent } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from '@/components/ui/alert-dialog'
import { StatusBadge } from '@/components/StatusBadge'
import { ResponseTimeChart } from '@/components/ResponseTimeChart'
import { SloCard } from '@/components/SloCard'
import { RecentChecksTable } from '@/components/RecentChecksTable'
import { useProbe, useDeleteProbe } from '@/api/hooks/useProbes'
import { useProbeChecks } from '@/api/hooks/useHealthChecks'
import { useSlos } from '@/api/hooks/useSlos'
import { useIncidents } from '@/api/hooks/useIncidents'
import { useAppContext } from '@/contexts/AppContext'
import { cn } from '@/lib/utils'

function pct95(values: number[]): number | null {
  if (values.length === 0) return null
  const sorted = [...values].sort((a, b) => a - b)
  const idx = Math.ceil(0.95 * sorted.length) - 1
  return sorted[Math.max(0, idx)] ?? null
}

function StatCard({
  label,
  value,
  sub,
  testId,
  valueClass,
}: {
  label: string
  value: React.ReactNode
  sub?: string
  testId?: string
  valueClass?: string
}) {
  return (
    <Card className="border-border" data-testid={testId}>
      <CardContent className="pt-4 pb-4">
        <p className="mb-2 text-[10px] uppercase tracking-widest text-muted-foreground/60">
          {label}
        </p>
        <div className={cn('font-mono text-xl font-semibold tabular-nums leading-none', valueClass)}>
          {value}
        </div>
        {sub && (
          <p className="mt-1.5 font-mono text-[10px] text-muted-foreground/60">{sub}</p>
        )}
      </CardContent>
    </Card>
  )
}

export function ProbeDetailPage() {
  const { id: probeId } = useParams<{ id: string }>()
  const { selectedProjectId: projectId } = useAppContext()
  const navigate = useNavigate()
  const [deleteOpen, setDeleteOpen] = useState(false)

  const { data: probe, isLoading } = useProbe(projectId, probeId ?? null)
  const { data: slos } = useSlos(projectId, probeId ?? null)
  const { data: incidents } = useIncidents(projectId, probeId ?? null)

  const { data: checks } = useProbeChecks(projectId, probeId ?? null, 24 * 60 * 60 * 1000)

  const deleteProbe = useDeleteProbe(projectId ?? '')

  const p95 = useMemo(
    () => pct95(checks.filter(c => c.isSuccess).map(c => c.responseTimeMs)),
    [checks],
  )

  const openIncidentCount = useMemo(
    () => incidents.filter(i => i.closedAt === null).length,
    [incidents],
  )

  const firstSlo = slos[0] ?? null

  function handleDelete() {
    if (!probeId || !projectId) return
    deleteProbe.mutate(probeId, {
      onSuccess: () => navigate('/dashboard'),
    })
  }

  if (!projectId) {
    return (
      <div className="flex flex-col items-center justify-center py-32 text-center">
        <p className="text-sm text-muted-foreground">Select a project in the header.</p>
      </div>
    )
  }

  if (isLoading || !probe) {
    return (
      <div className="space-y-6 animate-pulse">
        <div className="h-3 w-20 rounded bg-muted" />
        <div className="h-6 w-48 rounded bg-muted" />
        <div className="grid grid-cols-2 gap-4 lg:grid-cols-4">
          {[...Array(4)].map((_, i) => (
            <div key={i} className="h-[88px] rounded-lg bg-muted" />
          ))}
        </div>
        <div className="h-[300px] rounded-lg bg-muted" />
      </div>
    )
  }

  const probeStatus = probe.isActive ? 'healthy' as const : 'down' as const
  const lastChecked = probe.lastCheckedAt ?? probe.createdAt

  return (
    <div className="space-y-6">
      {/* Breadcrumb */}
      <Link
        to="/dashboard"
        className="inline-flex items-center gap-1.5 font-mono text-xs text-muted-foreground hover:text-foreground transition-colors"
      >
        <ArrowLeft className="h-3 w-3" />
        Dashboard
      </Link>

      {/* Page heading */}
      <div className="flex items-start justify-between gap-4">
        <div className="min-w-0 flex-1 space-y-1">
          <div className="flex items-center gap-3 flex-wrap">
            <h1 className="text-lg font-semibold tracking-tight text-foreground">{probe.name}</h1>
            <StatusBadge status={probeStatus} animated={probe.isActive} />
          </div>
          <p className="truncate font-mono text-xs text-muted-foreground">{probe.url}</p>
        </div>
        <Button
          variant="ghost"
          size="sm"
          className="h-8 gap-1.5 shrink-0 text-xs text-muted-foreground hover:text-destructive"
          onClick={() => setDeleteOpen(true)}
        >
          <Trash2 className="h-3.5 w-3.5" />
          Delete
        </Button>
      </div>

      {/* Stat cards */}
      <div className="grid gap-4 grid-cols-1 sm:grid-cols-2 lg:grid-cols-4">
        {/* Status */}
        <StatCard
          testId="stat-card-status"
          label="Current status"
          value={<StatusBadge status={probeStatus} animated={probe.isActive} />}
          sub={`checked ${formatDistanceToNow(new Date(lastChecked), { addSuffix: false })} ago`}
        />

        {/* p95 latency */}
        <StatCard
          label="p95 latency"
          value={p95 !== null ? `${p95}ms` : '—'}
          sub="24h window"
          valueClass={
            p95 === null
              ? 'text-muted-foreground'
              : p95 < 200
                ? 'text-emerald-600 dark:text-emerald-400'
                : p95 < 500
                  ? 'text-amber-500 dark:text-amber-400'
                  : 'text-red-500 dark:text-red-400'
          }
        />

        {/* Availability */}
        <StatCard
          label="Availability"
          value={
            firstSlo?.latestMeasurement
              ? `${firstSlo.latestMeasurement.availabilityPct.toFixed(2)}%`
              : '—'
          }
          sub={firstSlo ? `${firstSlo.windowDays}d window` : 'no SLO defined'}
          valueClass={
            !firstSlo?.latestMeasurement
              ? 'text-muted-foreground'
              : firstSlo.latestMeasurement.availabilityPct >= firstSlo.targetAvailabilityPct
                ? 'text-emerald-600 dark:text-emerald-400'
                : 'text-red-500 dark:text-red-400'
          }
        />

        {/* Open incidents */}
        <StatCard
          label="Open incidents"
          value={String(openIncidentCount)}
          sub="active incidents"
          valueClass={
            openIncidentCount === 0
              ? 'text-muted-foreground'
              : 'text-red-500 dark:text-red-400'
          }
        />
      </div>

      {/* Response time chart */}
      <div className="rounded-lg border border-border p-5">
        <ResponseTimeChart probeId={probe.id} projectId={projectId} />
      </div>

      {/* SLO + recent checks */}
      <div className="grid gap-4 grid-cols-1 lg:grid-cols-[320px,1fr]">
        {firstSlo ? (
          <SloCard slo={firstSlo} />
        ) : (
          <Card className="border-border">
            <CardContent className="flex h-full min-h-[120px] items-center justify-center">
              <p className="text-xs text-muted-foreground">No SLO defined for this probe.</p>
            </CardContent>
          </Card>
        )}

        <div>
          <p className="mb-3 text-[10px] uppercase tracking-widest text-muted-foreground/70">
            Recent checks
          </p>
          <RecentChecksTable probeId={probe.id} projectId={projectId} />
        </div>
      </div>

      {/* Delete dialog */}
      <AlertDialog open={deleteOpen} onOpenChange={setDeleteOpen}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Delete probe?</AlertDialogTitle>
            <AlertDialogDescription>
              This will permanently delete{' '}
              <span className="font-medium text-foreground">{probe.name}</span> and all its
              health check history. This action cannot be undone.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel className="text-xs">Cancel</AlertDialogCancel>
            <AlertDialogAction
              className="bg-destructive text-destructive-foreground hover:bg-destructive/90 text-xs"
              onClick={handleDelete}
            >
              {deleteProbe.isPending ? 'Deleting…' : 'Delete'}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  )
}
