import { type ReactNode } from 'react'
import { Sun, Moon, Terminal } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { useTheme } from '@/contexts/ThemeContext'
import { OrgProjectSwitcher } from '@/components/OrgProjectSwitcher'
import { useCommandPalette } from '@/components/CommandPalette'
import { cn } from '@/lib/utils'

export type ConnectionStatus = 'connected' | 'connecting' | 'disconnected'

interface ConnectionStatusDotProps {
  status: ConnectionStatus
}

function ConnectionStatusDot({ status }: ConnectionStatusDotProps) {
  return (
    <div className="relative flex h-5 w-5 items-center justify-center" role="img" aria-label={`SignalR ${status}`}>
      <div
        className={cn('h-1.5 w-1.5 rounded-full', {
          'bg-emerald-500': status === 'connected',
          'bg-amber-400': status === 'connecting',
          'bg-red-500': status === 'disconnected',
        })}
      />
      {status === 'connected' && (
        <div className="absolute h-1.5 w-1.5 rounded-full bg-emerald-500 animate-ping opacity-60" />
      )}
      {status === 'connecting' && (
        <div className="absolute h-1.5 w-1.5 rounded-full bg-amber-400 animate-pulse opacity-60" />
      )}
    </div>
  )
}

interface AppShellProps {
  children: ReactNode
  connectionStatus?: ConnectionStatus
}

export function AppShell({
  children,
  connectionStatus = 'connecting',
}: AppShellProps) {
  const { theme, toggleTheme } = useTheme()
  const { openPalette } = useCommandPalette()

  return (
    <div className="min-h-screen bg-background">
      <header className="sticky top-0 z-50 h-14 w-full border-b border-border bg-background/80 backdrop-blur-sm">
        <div className="flex h-full max-w-screen-xl mx-auto items-center gap-3 px-6">
          {/* Wordmark */}
          <div className="flex items-center gap-2 shrink-0">
            <div className="flex h-5 w-5 items-center justify-center rounded border border-border">
              <div className="h-1.5 w-1.5 rounded-full bg-foreground" />
            </div>
            <span className="font-mono text-[11px] tracking-[0.18em] uppercase text-muted-foreground select-none">
              PulseWatch
            </span>
          </div>

          {/* Divider */}
          <div className="h-4 w-px bg-border shrink-0" />

          {/* Workspace switcher */}
          <OrgProjectSwitcher />

          {/* Spacer */}
          <div className="flex-1" />

          {/* Right controls */}
          <div className="flex items-center gap-0.5">
            <ConnectionStatusDot status={connectionStatus} />

            <Button
              variant="ghost"
              size="icon"
              className="h-8 w-8 text-muted-foreground hover:text-foreground"
              onClick={toggleTheme}
              aria-label="Toggle theme"
            >
              {theme === 'dark' ? (
                <Sun className="h-3.5 w-3.5" />
              ) : (
                <Moon className="h-3.5 w-3.5" />
              )}
            </Button>

            <Button
              variant="ghost"
              size="sm"
              className="h-8 gap-1.5 px-2 font-mono text-[11px] text-muted-foreground hover:text-foreground"
              onClick={openPalette}
              aria-label="Open command palette"
            >
              <Terminal className="h-3.5 w-3.5" />
              <span className="hidden sm:inline tracking-wide">⌘K</span>
            </Button>
          </div>
        </div>
      </header>

      <main className="max-w-screen-xl mx-auto px-6 py-8">
        {children}
      </main>
    </div>
  )
}
