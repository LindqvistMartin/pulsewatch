import { createContext, useContext, useState, useEffect, type ReactNode } from 'react'
import { useNavigate } from 'react-router-dom'
import { Plus, LayoutDashboard, Sun } from 'lucide-react'
import {
  Command,
  CommandEmpty,
  CommandGroup,
  CommandInput,
  CommandItem,
  CommandList,
} from 'cmdk'
import {
  Dialog,
  DialogContent,
  DialogTitle,
} from '@/components/ui/dialog'
import { useAddProbeDialog } from '@/components/AddProbeDialog'
import { useTheme } from '@/contexts/ThemeContext'

// ─── Context ──────────────────────────────────────────────────────────────────

interface CommandPaletteContextValue {
  openPalette: () => void
}

const CommandPaletteContext = createContext<CommandPaletteContextValue | null>(null)

export function useCommandPalette() {
  const ctx = useContext(CommandPaletteContext)
  if (!ctx) throw new Error('useCommandPalette must be inside CommandPaletteProvider')
  return ctx
}

// ─── Provider ────────────────────────────────────────────────────────────────

export function CommandPaletteProvider({ children }: { children: ReactNode }) {
  const [open, setOpen] = useState(false)

  useEffect(() => {
    function onKeyDown(e: KeyboardEvent) {
      if ((e.metaKey || e.ctrlKey) && e.key === 'k') {
        e.preventDefault()
        setOpen(prev => !prev)
      }
    }
    document.addEventListener('keydown', onKeyDown)
    return () => document.removeEventListener('keydown', onKeyDown)
  }, [])

  return (
    <CommandPaletteContext.Provider value={{ openPalette: () => setOpen(true) }}>
      {children}
      <PaletteDialog open={open} onOpenChange={setOpen} />
    </CommandPaletteContext.Provider>
  )
}

// ─── Dialog ──────────────────────────────────────────────────────────────────

interface PaletteDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
}

function PaletteDialog({ open, onOpenChange }: PaletteDialogProps) {
  const navigate = useNavigate()
  const { open: openAddProbe } = useAddProbeDialog()
  const { theme, toggleTheme } = useTheme()

  function close() {
    onOpenChange(false)
  }

  function run(fn: () => void) {
    fn()
    close()
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent
        className="overflow-hidden p-0 shadow-2xl [&>button]:hidden sm:max-w-lg"
      >
        {/* DialogTitle inside DialogContent for correct aria-labelledby association */}
        <DialogTitle className="sr-only">Command palette</DialogTitle>
        <Command
          className={[
            'flex flex-col rounded-lg bg-card',
            '[&_[cmdk-group-heading]]:px-3 [&_[cmdk-group-heading]]:pt-3 [&_[cmdk-group-heading]]:pb-1',
            '[&_[cmdk-group-heading]]:text-[10px] [&_[cmdk-group-heading]]:uppercase',
            '[&_[cmdk-group-heading]]:tracking-widest [&_[cmdk-group-heading]]:text-muted-foreground/60',
            '[&_[cmdk-input-wrapper]]:flex [&_[cmdk-input-wrapper]]:items-center',
            '[&_[cmdk-input-wrapper]]:border-b [&_[cmdk-input-wrapper]]:border-border',
            '[&_[cmdk-input-wrapper]]:px-3',
          ].join(' ')}
        >
          <CommandInput
            placeholder="Search commands..."
            className={[
              'h-12 flex-1 bg-transparent font-mono text-sm outline-none',
              'placeholder:text-muted-foreground/50',
            ].join(' ')}
          />
          <CommandList className="max-h-[320px] overflow-y-auto px-1 pb-2">
            <CommandEmpty className="py-8 text-center font-mono text-xs text-muted-foreground">
              No commands found.
            </CommandEmpty>

            <CommandGroup heading="Probes">
              <PaletteItem
                icon={<Plus className="h-3.5 w-3.5" />}
                label="Add probe"
                shortcut="A"
                onSelect={() => run(openAddProbe)}
              />
            </CommandGroup>

            <CommandGroup heading="Navigation">
              <PaletteItem
                icon={<LayoutDashboard className="h-3.5 w-3.5" />}
                label="Go to dashboard"
                onSelect={() => run(() => navigate('/dashboard'))}
              />
            </CommandGroup>

            <CommandGroup heading="Theme">
              <PaletteItem
                icon={<Sun className="h-3.5 w-3.5" />}
                label={`Switch to ${theme === 'dark' ? 'light' : 'dark'} mode`}
                onSelect={() => run(toggleTheme)}
              />
            </CommandGroup>
          </CommandList>
        </Command>
      </DialogContent>
    </Dialog>
  )
}

interface PaletteItemProps {
  icon: ReactNode
  label: string
  shortcut?: string
  onSelect: () => void
}

function PaletteItem({ icon, label, shortcut, onSelect }: PaletteItemProps) {
  return (
    <CommandItem
      className={[
        'flex cursor-pointer items-center gap-2.5 rounded-md px-3 py-2.5',
        'text-sm text-foreground outline-none',
        'data-[selected=true]:bg-accent data-[selected=true]:text-accent-foreground',
        'transition-colors duration-75',
      ].join(' ')}
      onSelect={onSelect}
    >
      <span className="text-muted-foreground">{icon}</span>
      <span className="flex-1 font-mono text-xs">{label}</span>
      {shortcut && (
        <kbd className="rounded border border-border bg-muted/60 px-1.5 font-mono text-[10px] text-muted-foreground">
          {shortcut}
        </kbd>
      )}
    </CommandItem>
  )
}
