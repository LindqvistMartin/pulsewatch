import { Check, ChevronDown } from 'lucide-react'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuGroup,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import { Button } from '@/components/ui/button'
import { useAppContext } from '@/contexts/AppContext'
import { useOrganizations } from '@/api/hooks/useOrganizations'
import { useProjects } from '@/api/hooks/useProjects'
import { cn } from '@/lib/utils'

export function OrgProjectSwitcher() {
  const { selectedOrgId, selectedProjectId, setSelectedOrg, setSelectedProject } = useAppContext()
  const { data: orgs } = useOrganizations()
  const { data: projects } = useProjects(selectedOrgId)

  const selectedOrg = orgs.find(o => o.id === selectedOrgId)
  const selectedProject = projects.find(p => p.id === selectedProjectId)

  const label =
    selectedOrg && selectedProject
      ? `${selectedOrg.name} / ${selectedProject.name}`
      : selectedOrg
        ? selectedOrg.name
        : 'Select workspace'

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button
          variant="ghost"
          size="sm"
          className="h-7 gap-1.5 px-2 font-mono text-xs text-muted-foreground hover:text-foreground"
        >
          <span className="max-w-[180px] truncate">{label}</span>
          <ChevronDown className="h-3 w-3 shrink-0 opacity-50" />
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="start" className="w-60">
        <DropdownMenuLabel className="font-normal text-xs text-muted-foreground">
          Organizations
        </DropdownMenuLabel>
        <DropdownMenuGroup>
          {orgs.length === 0 ? (
            <DropdownMenuItem disabled className="text-xs text-muted-foreground italic">
              No organizations yet
            </DropdownMenuItem>
          ) : (
            orgs.map(org => (
              <DropdownMenuItem
                key={org.id}
                className="text-xs"
                onSelect={() => setSelectedOrg(org.id)}
              >
                <Check
                  className={cn(
                    'mr-2 h-3 w-3 shrink-0',
                    selectedOrgId === org.id ? 'opacity-100' : 'opacity-0',
                  )}
                />
                {org.name}
              </DropdownMenuItem>
            ))
          )}
        </DropdownMenuGroup>

        {selectedOrgId && (
          <>
            <DropdownMenuSeparator />
            <DropdownMenuLabel className="font-normal text-xs text-muted-foreground">
              Projects
            </DropdownMenuLabel>
            <DropdownMenuGroup>
              {projects.length === 0 ? (
                <DropdownMenuItem disabled className="text-xs text-muted-foreground italic">
                  No projects in this organization
                </DropdownMenuItem>
              ) : (
                projects.map(project => (
                  <DropdownMenuItem
                    key={project.id}
                    className="text-xs"
                    onSelect={() => setSelectedProject(project.id)}
                  >
                    <Check
                      className={cn(
                        'mr-2 h-3 w-3 shrink-0',
                        selectedProjectId === project.id ? 'opacity-100' : 'opacity-0',
                      )}
                    />
                    {project.name}
                  </DropdownMenuItem>
                ))
              )}
            </DropdownMenuGroup>
          </>
        )}
      </DropdownMenuContent>
    </DropdownMenu>
  )
}
