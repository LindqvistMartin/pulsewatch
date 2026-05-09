import { createContext, useContext, useState, type ReactNode } from 'react'

interface AppContextValue {
  selectedOrgId: string | null
  selectedProjectId: string | null
  setSelectedOrg: (id: string | null) => void
  setSelectedProject: (id: string | null) => void
}

const AppContext = createContext<AppContextValue | null>(null)

export function AppContextProvider({ children }: { children: ReactNode }) {
  const [selectedOrgId, setSelectedOrgId] = useState<string | null>(
    () => localStorage.getItem('pw-org-id')
  )
  const [selectedProjectId, setSelectedProjectId] = useState<string | null>(
    () => localStorage.getItem('pw-project-id')
  )

  function setSelectedOrg(id: string | null) {
    setSelectedOrgId(id)
    if (id) localStorage.setItem('pw-org-id', id)
    else localStorage.removeItem('pw-org-id')
    setSelectedProjectId(null)
    localStorage.removeItem('pw-project-id')
  }

  function setSelectedProject(id: string | null) {
    setSelectedProjectId(id)
    if (id) localStorage.setItem('pw-project-id', id)
    else localStorage.removeItem('pw-project-id')
  }

  return (
    <AppContext.Provider value={{ selectedOrgId, selectedProjectId, setSelectedOrg, setSelectedProject }}>
      {children}
    </AppContext.Provider>
  )
}

export function useAppContext() {
  const ctx = useContext(AppContext)
  if (!ctx) throw new Error('useAppContext must be inside AppContextProvider')
  return ctx
}
