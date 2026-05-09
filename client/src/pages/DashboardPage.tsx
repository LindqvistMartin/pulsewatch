import { Plus, FolderOpen } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { ProbeTable } from '@/components/ProbeTable'
import { useAppContext } from '@/contexts/AppContext'
import { useOrganizations } from '@/api/hooks/useOrganizations'
import { useProjects } from '@/api/hooks/useProjects'
import { useProbes } from '@/api/hooks/useProbes'
import { useAddProbeDialog } from '@/components/AddProbeDialog'
import { usePulseConnection } from '@/hooks/usePulseConnection'

export function DashboardPage() {
  const { selectedOrgId, selectedProjectId } = useAppContext()
  const { data: orgs } = useOrganizations()
  const { data: projects } = useProjects(selectedOrgId)
  const { data: probes, isFetching } = useProbes(selectedProjectId)
  const { open: openAddProbe } = useAddProbeDialog()

  usePulseConnection(selectedProjectId)

  const selectedProject = projects.find(p => p.id === selectedProjectId)
  const selectedOrg = orgs.find(o => o.id === selectedOrgId)

  if (!selectedProjectId) {
    return (
      <div className="flex flex-col items-center justify-center py-32 text-center">
        <div className="mb-4 flex h-12 w-12 items-center justify-center rounded-full border border-border bg-muted/20">
          <FolderOpen className="h-5 w-5 text-muted-foreground/50" />
        </div>
        <p className="text-sm font-medium text-foreground">No workspace selected</p>
        <p className="mt-1.5 max-w-xs text-xs text-muted-foreground leading-relaxed">
          Use the workspace selector in the header to choose an organization and project, then you can manage probes here.
        </p>
      </div>
    )
  }

  const isInitialLoad = isFetching && probes.length === 0

  return (
    <div className="space-y-6">
      {/* Page header */}
      <div className="flex items-start justify-between gap-4">
        <div>
          <h1 className="text-lg font-semibold tracking-tight text-foreground">
            Probes
          </h1>
          {(selectedOrg || selectedProject) && (
            <p className="mt-0.5 font-mono text-xs text-muted-foreground">
              {selectedOrg?.name}
              {selectedOrg && selectedProject && (
                <span className="mx-1.5 opacity-40">/</span>
              )}
              {selectedProject?.name}
            </p>
          )}
        </div>

        <Button
          size="sm"
          className="gap-1.5 font-mono text-xs shrink-0"
          onClick={openAddProbe}
        >
          <Plus className="h-3.5 w-3.5" />
          Add probe
        </Button>
      </div>

      {/* Probe table */}
      <ProbeTable
        probes={probes}
        projectId={selectedProjectId}
        loading={isInitialLoad}
        onAddProbe={openAddProbe}
      />
    </div>
  )
}
