import { Routes, Route, Navigate } from 'react-router-dom'
import { AppShell } from '@/components/AppShell'
import { AddProbeDialogProvider } from '@/components/AddProbeDialog'
import { CommandPaletteProvider } from '@/components/CommandPalette'
import { DashboardPage } from '@/pages/DashboardPage'
import { ProbeDetailPage } from '@/pages/ProbeDetailPage'
import { StatusPagePage } from '@/pages/StatusPagePage'

export default function App() {
  return (
    <AddProbeDialogProvider>
      <CommandPaletteProvider>
        <Routes>
          <Route path="/" element={<Navigate to="/dashboard" replace />} />
          <Route
            path="/dashboard"
            element={
              <AppShell>
                <DashboardPage />
              </AppShell>
            }
          />
          <Route
            path="/probes/:id"
            element={
              <AppShell>
                <ProbeDetailPage />
              </AppShell>
            }
          />
          <Route path="/p/:slug" element={<StatusPagePage />} />
        </Routes>
      </CommandPaletteProvider>
    </AddProbeDialogProvider>
  )
}
