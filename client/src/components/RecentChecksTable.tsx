import { useMemo } from 'react'
import { formatDistanceToNow } from 'date-fns'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import { StatusBadge } from '@/components/StatusBadge'
import { useProbeChecks } from '@/api/hooks/useHealthChecks'
import { cn } from '@/lib/utils'

interface RecentChecksTableProps {
  projectId: string
  probeId: string
}

const DAY_MS = 24 * 60 * 60 * 1000

export function RecentChecksTable({ projectId, probeId }: RecentChecksTableProps) {
  const { data: checks } = useProbeChecks(projectId, probeId, DAY_MS)

  const sorted = useMemo(
    () =>
      [...checks]
        .sort((a, b) => new Date(b.checkedAt).getTime() - new Date(a.checkedAt).getTime())
        .slice(0, 50),
    [checks],
  )

  if (sorted.length === 0) {
    return (
      <div className="flex h-32 items-center justify-center rounded-lg border border-dashed border-border">
        <p className="text-xs text-muted-foreground">No checks yet</p>
      </div>
    )
  }

  return (
    <div className="overflow-hidden rounded-lg border border-border">
      <Table>
        <TableHeader>
          <TableRow className="border-border/50 hover:bg-transparent">
            <TableHead className="w-[130px] text-[10px] uppercase tracking-widest text-muted-foreground/70">
              Time
            </TableHead>
            <TableHead className="w-[100px] text-[10px] uppercase tracking-widest text-muted-foreground/70">
              Status
            </TableHead>
            <TableHead className="w-[60px] text-[10px] uppercase tracking-widest text-muted-foreground/70">
              Code
            </TableHead>
            <TableHead className="w-[100px] text-[10px] uppercase tracking-widest text-muted-foreground/70">
              Response
            </TableHead>
            <TableHead className="text-[10px] uppercase tracking-widest text-muted-foreground/70">
              Failure
            </TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {sorted.map(check => {
            const code = check.statusCode
            const codeClass =
              code !== null
                ? code >= 200 && code < 300
                  ? 'text-emerald-600 dark:text-emerald-400'
                  : 'text-red-500 dark:text-red-400'
                : 'text-muted-foreground'

            const responseClass =
              check.responseTimeMs < 200
                ? 'text-emerald-600 dark:text-emerald-400'
                : check.responseTimeMs < 500
                  ? 'text-amber-500 dark:text-amber-400'
                  : 'text-red-500 dark:text-red-400'

            return (
              <TableRow
                key={check.id}
                className="border-border/50 hover:bg-muted/20 transition-colors duration-75"
              >
                <TableCell className="py-2.5 font-mono text-xs text-muted-foreground tabular-nums">
                  {formatDistanceToNow(new Date(check.checkedAt), { addSuffix: false })} ago
                </TableCell>
                <TableCell className="py-2.5">
                  <StatusBadge status={check.isSuccess ? 'healthy' : 'down'} />
                </TableCell>
                <TableCell className="py-2.5">
                  <span className={cn('font-mono text-xs tabular-nums', codeClass)}>
                    {code ?? '—'}
                  </span>
                </TableCell>
                <TableCell className="py-2.5">
                  <span className={cn('font-mono text-xs tabular-nums', responseClass)}>
                    {check.responseTimeMs}ms
                  </span>
                </TableCell>
                <TableCell className="py-2.5 max-w-[200px]">
                  {check.failureReason && (
                    <span className="block truncate font-mono text-[10px] text-muted-foreground/60">
                      {check.failureReason.length > 60
                        ? `${check.failureReason.slice(0, 60)}…`
                        : check.failureReason}
                    </span>
                  )}
                </TableCell>
              </TableRow>
            )
          })}
        </TableBody>
      </Table>
    </div>
  )
}
