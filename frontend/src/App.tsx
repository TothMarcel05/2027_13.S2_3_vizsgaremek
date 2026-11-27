import { useState } from 'react'
import { Navigate, Route, Routes, useLocation, useNavigate } from 'react-router'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import { AnimatePresence, motion } from 'framer-motion'
import { Compass } from 'lucide-react'
import { tokenStore } from './api/client'
import { authApi, meApi, workspaceApi } from './api/services'
import type { ID } from './api/types'
import { WorkspaceContext } from './lib/data'
import { useEdition } from './editions'
import { Button, EmptyState, ErrorState, Skeleton, SkeletonGrid } from './components/ui/core'
import { SHELLS } from './components/layout/Shells'
import { Login, Register, ResetPassword, TwoFactor } from './features/auth/Auth'
import { Dashboard } from './features/dashboard/Dashboard'
import { Projects } from './features/projects/Projects'
import { ProjectDetail } from './features/projects/ProjectDetail'
import { Chat } from './features/chat/Chat'
import { CalendarView } from './features/calendar/Calendar'
import { Files } from './features/files/Files'
import { Trash } from './features/trash/Trash'
import { Profile } from './features/settings/Profile'
import { WorkspaceSettings } from './features/settings/WorkspaceSettings'
import { Admin } from './features/admin/Admin'

const Loading = () => (
  <div className="mx-auto max-w-5xl space-y-5 p-6" role="status" aria-label="Loading workspace">
    <Skeleton className="h-14" /><Skeleton className="h-10 w-1/2" /><SkeletonGrid />
  </div>
)

function Workspace({ signOut }: { signOut: () => void }) {
  const location = useLocation()
  const navigate = useNavigate()
  const edition = useEdition()
  const Shell = SHELLS[edition.id]
  const [selected, setSelected] = useState<ID | null>(() => localStorage.getItem('ws.current'))
  const workspaces = useQuery({ queryKey: ['workspaces'], queryFn: workspaceApi.list })
  const me = useQuery({ queryKey: ['me'], queryFn: meApi.get })

  if (workspaces.isError) return <ErrorState error={workspaces.error} retry={() => workspaces.refetch()} />
  const list = workspaces.data
  if (!list || !me.data) return <Loading />
  const workspace = list.find((w) => w.id === selected) ?? list[0]
  if (!workspace) return <EmptyState icon={<Compass size={20} />} title="No workspaces" action={<Button onClick={signOut}>Sign out</Button>} />
  const setWorkspace = (id: ID) => { localStorage.setItem('ws.current', id); setSelected(id) }
  // Moving between two projects keeps the same page; switching section animates.
  const section = location.pathname.split('/').slice(0, 3).join('/')

  return (
    <WorkspaceContext.Provider value={{ workspace, workspaces: list, setWorkspace }}>
      <Shell signOut={signOut}>
        <AnimatePresence mode="wait" initial={false}>
          <motion.div key={`${workspace.id}:${section}`} initial="hidden" animate="show" exit="exit" variants={edition.page}
            onAnimationStart={(definition) => { if (definition === 'show') window.scrollTo(0, 0) }}>
            <Routes location={location}>
              <Route path="/" element={<Dashboard />} />
              <Route path="/projects" element={<Projects />} />
              <Route path="/projects/:projectId" element={<ProjectDetail />} />
              <Route path="/chat" element={<Chat />} />
              <Route path="/calendar" element={<CalendarView />} />
              <Route path="/files" element={<Files />} />
              <Route path="/trash" element={<Trash />} />
              <Route path="/profile" element={<Profile />} />
              <Route path="/workspace" element={<WorkspaceSettings />} />
              <Route path="/admin" element={me.data.systemRole === 'super_admin' ? <Admin /> : <Navigate to="/" replace />} />
              <Route path="*" element={<EmptyState icon={<Compass size={20} />} title="Page not found" action={<Button variant="primary" onClick={() => navigate('/')}>Back to overview</Button>} />} />
            </Routes>
          </motion.div>
        </AnimatePresence>
      </Shell>
    </WorkspaceContext.Provider>
  )
}

const AUTH_PATHS = ['/login', '/register', '/reset', '/two-factor']

export function App() {
  const [token, setToken] = useState(tokenStore.get)
  const client = useQueryClient()
  const navigate = useNavigate()
  const { pathname } = useLocation()
  const signIn = (value: string) => { tokenStore.set(value); client.clear(); setToken(value) }
  const signOut = () => {
    authApi.logout().catch(() => {})
    tokenStore.clear(); setToken(null); client.clear(); navigate('/login')
  }
  if (token) return AUTH_PATHS.includes(pathname) ? <Navigate to="/" replace /> : <Workspace signOut={signOut} />
  return (
    <Routes>
      <Route path="/login" element={<Login signIn={signIn} />} />
      <Route path="/register" element={<Register signIn={signIn} />} />
      <Route path="/reset" element={<ResetPassword />} />
      <Route path="/two-factor" element={<TwoFactor signIn={signIn} />} />
      <Route path="*" element={<Navigate to="/login" replace />} />
    </Routes>
  )
}
