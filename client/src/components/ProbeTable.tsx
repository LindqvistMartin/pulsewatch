import { useState } from 'react'
import { Link } from 'react-router-dom'
import { MoreHorizontal, Trash2, Plus } from 'lucide-react'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
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
import {
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from '@/components/ui/tooltip'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { StatusBadge, type ProbeStatus } from '@/components/StatusBadge'
import { useDeleteProbe } from '@/api/hooks/useProbes'
import { cn } from '@/lib/utils'
import type { Probe } from '@/api/types'

interface ProbeTableProps {
  probes: Probe[]
  projectId: string
  loading?: boolean
  onAddProbe: () => void
}

function SkeletonRow() {
  return (
    <TableRow className="border-border/50">
      {[40, 120, 200, 50, 40, 30].map((w, i) => (
        <TableCell key={i}>
          <div
            className="h-3 animate-pulse rounded bg-muted"
            style={{ width: w }}
          />
        </TableCell>
      ))}
    </TableRow>
  )
}

function truncateUrl(url: string, max = 50) {
  if (url.length <= max) return url
  try {
    const u = new URL(url)
    const path = u.pathname + u.search
    const host = u.hostname
    if (host.length + 3 >= max) return url.slice(0, max) + '…'
    const remaining = max - host.length - 3
    return `${u.protocol}//${host}${path.length > remaining ? path.slice(0, remaining) + '…' : path}`
  } catch {
    return url.slice(0, max) + '…'
  }
}

export function ProbeTable({ probes, projectId, loading = false, onAddProbe }: ProbeTableProps) {
  const [deleteTarget, setDeleteTarget] = useState<Probe | null>(null)
  const deleteProbe = useDeleteProbe(projectId)

  function handleConfirmDelete() {
    if (!deleteTarget) return
    deleteProbe.mutate(deleteTarget.id, {
      onSuccess: () => setDeleteTarget(null),
    })
  }

  if (loading) {
    return (
      <div className="rounded-lg border border-border overflow-hidden">
        <Table data-testid="probe-table">
          <TableHeader>
            <TableRow className="border-border/50 hover:bg-transparent">
              <TableHead className="w-[100px] text-[10px] uppercase tracking-widest text-muted-foreground/70">Status</TableHead>
              <TableHead className="text-[10px] uppercase tracking-widest text-muted-foreground/70">Name</TableHead>
              <TableHead className="text-[10px] uppercase tracking-widest text-muted-foreground/70">Endpoint</TableHead>
              <TableHead className="hidden sm:table-cell text-[10px] uppercase tracking-widest text-muted-foreground/70">Interval</TableHead>
              <TableHead className="hidden sm:table-cell text-[10px] uppercase tracking-widest text-muted-foreground/70">Method</TableHead>
              <TableHead className="w-10" />
            </TableRow>
          </TableHeader>
          <TableBody>
            <SkeletonRow />
            <SkeletonRow />
            <SkeletonRow />
          </TableBody>
        </Table>
      </div>
    )
  }

  if (probes.length === 0) {
    return (
      <div className="flex flex-col items-center justify-center rounded-lg border border-dashed border-border py-20 text-center">
        <div className="mb-4 flex h-10 w-10 items-center justify-center rounded-full border border-border bg-muted/30">
          <div className="h-1.5 w-1.5 rounded-full bg-muted-foreground/40" />
        </div>
        <p className="text-sm font-medium text-foreground">No probes configured</p>
        <p className="mt-1 text-xs text-muted-foreground">
          Add your first probe with the button above or press{' '}
          <kbd className="rounded border border-border bg-muted px-1 font-mono text-[10px]">⌘K</kbd>
        </p>
        <Button
          variant="outline"
          size="sm"
          className="mt-6 gap-1.5 font-mono text-xs"
          onClick={onAddProbe}
        >
          <Plus className="h-3 w-3" />
          Add probe
        </Button>
      </div>
    )
  }

  return (
    <TooltipProvider delayDuration={400}>
      <div className="rounded-lg border border-border overflow-hidden">
        <Table data-testid="probe-table">
          <TableHeader>
            <TableRow className="border-border/50 hover:bg-transparent">
              <TableHead className="w-[110px] text-[10px] uppercase tracking-widest text-muted-foreground/70">
                Status
              </TableHead>
              <TableHead className="min-w-[140px] text-[10px] uppercase tracking-widest text-muted-foreground/70">
                Name
              </TableHead>
              <TableHead className="text-[10px] uppercase tracking-widest text-muted-foreground/70">
                Endpoint
              </TableHead>
              <TableHead className="hidden sm:table-cell w-[80px] text-[10px] uppercase tracking-widest text-muted-foreground/70">
                Interval
              </TableHead>
              <TableHead className="hidden sm:table-cell w-[70px] text-[10px] uppercase tracking-widest text-muted-foreground/70">
                Method
              </TableHead>
              <TableHead className="w-10" />
            </TableRow>
          </TableHeader>
          <TableBody>
            {probes.map(probe => {
              const status: ProbeStatus =
                !probe.isActive ? 'down'
                : probe.lastCheckSuccess === null ? 'unknown'
                : probe.lastCheckSuccess ? 'healthy'
                : 'down'
              const short = truncateUrl(probe.url)
              const needsTooltip = short !== probe.url

              return (
                <TableRow
                  key={probe.id}
                  className={cn(
                    'border-border/50 transition-colors duration-100',
                    'hover:bg-muted/30',
                  )}
                >
                  <TableCell className="py-3">
                    <StatusBadge status={status} animated={probe.isActive} />
                  </TableCell>

                  <TableCell className="py-3 font-medium">
                    <Link
                      to={`/probes/${probe.id}`}
                      className="text-sm hover:underline underline-offset-2 decoration-muted-foreground/40"
                    >
                      {probe.name}
                    </Link>
                  </TableCell>

                  <TableCell className="py-3">
                    {needsTooltip ? (
                      <Tooltip>
                        <TooltipTrigger asChild>
                          <span className="cursor-default font-mono text-xs text-muted-foreground">
                            {short}
                          </span>
                        </TooltipTrigger>
                        <TooltipContent side="bottom" className="font-mono text-xs max-w-xs break-all">
                          {probe.url}
                        </TooltipContent>
                      </Tooltip>
                    ) : (
                      <span className="font-mono text-xs text-muted-foreground">{probe.url}</span>
                    )}
                  </TableCell>

                  <TableCell className="hidden sm:table-cell py-3">
                    <span className="font-mono text-xs text-muted-foreground tabular-nums">
                      {probe.intervalSeconds}s
                    </span>
                  </TableCell>

                  <TableCell className="hidden sm:table-cell py-3">
                    <Badge
                      variant="outline"
                      className="font-mono text-[10px] uppercase tracking-wide h-5 px-1.5"
                    >
                      {probe.method}
                    </Badge>
                  </TableCell>

                  <TableCell className="py-3">
                    <DropdownMenu>
                      <DropdownMenuTrigger asChild>
                        <Button
                          variant="ghost"
                          size="icon"
                          className="h-7 w-7 text-muted-foreground hover:text-foreground"
                        >
                          <MoreHorizontal className="h-3.5 w-3.5" />
                          <span className="sr-only">Actions</span>
                        </Button>
                      </DropdownMenuTrigger>
                      <DropdownMenuContent align="end" className="w-36">
                        <DropdownMenuItem
                          className="text-xs text-destructive focus:text-destructive gap-2"
                          onSelect={() => setDeleteTarget(probe)}
                        >
                          <Trash2 className="h-3 w-3" />
                          Delete probe
                        </DropdownMenuItem>
                      </DropdownMenuContent>
                    </DropdownMenu>
                  </TableCell>
                </TableRow>
              )
            })}
          </TableBody>
        </Table>
      </div>

      <AlertDialog
        open={deleteTarget !== null}
        onOpenChange={open => !open && setDeleteTarget(null)}
      >
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Delete probe?</AlertDialogTitle>
            <AlertDialogDescription>
              This will permanently delete{' '}
              <span className="font-medium text-foreground">
                {deleteTarget?.name}
              </span>
              {' '}and all its health check history. This action cannot be undone.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel className="text-xs">Cancel</AlertDialogCancel>
            <AlertDialogAction
              className="bg-destructive text-destructive-foreground hover:bg-destructive/90 text-xs"
              onClick={handleConfirmDelete}
            >
              {deleteProbe.isPending ? 'Deleting…' : 'Delete'}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </TooltipProvider>
  )
}
