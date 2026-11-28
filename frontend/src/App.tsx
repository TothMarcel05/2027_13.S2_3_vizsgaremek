import { useEffect, useState } from 'react'
import { Navigate, Route, Routes, useLocation, useNavigate } from 'react-router'
import { useQuery } from '@tanstack/react-query'
import { AnimatePresence, motion } from 'framer-motion'
import { Compass } from 'lucide-react'
import { authApi } from './api/auth'
import { workspaceKeys, workspaceService } from './api/workspaces'
import type { ID } from './api/types'
import { WorkspaceContext, useMeQuery } from './lib/data'
import { MOTION } from './lib/motion'
import { Button, EmptyState, ErrorState, Skeleton, SkeletonGrid } from './components/ui/core'
import { Shell } from './components/layout/Shells'
import { CreateWorkspaceModal } from './components/layout/CreateWorkspaceModal'
import { useToast } from './components/ui/overlay'
import { ForgotPassword, Login, Register, ResetPassword, TwoFactor } from './features/auth/Auth'
import { useSession } from './features/auth/useSession'
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

/** For an account with no workspace yet, such as one that has just registered. */
function NoWorkspace({ onCreated, signOut }: { onCreated: (id: ID) => void; signOut: () => void }) {
  const [creating, setCreating] = useState(false)
  return (
    <main className="flex min-h-dvh flex-col items-center justify-center">
      <EmptyState icon={<Compass size={20} />} title="No workspaces yet" action={
        <div className="flex flex-wrap justify-center gap-2">
          <Button variant="primary" onClick={() => setCreating(true)}>Create workspace</Button>
          <Button variant="ghost" onClick={signOut}>Sign out</Button>
        </div>
      }>Create one to get started.</EmptyState>
      <CreateWorkspaceModal open={creating} onClose={() => setCreating(false)} onCreated={onCreated} />
    </main>
  )
}

function Workspace({ signOut }: { signOut: () => void }) {
  const location = useLocation()
  const navigate = useNavigate()
  const [selected, setSelected] = useState<ID | null>(() => localStorage.getItem('ws.current'))
  const workspaces = useQuery({ queryKey: workspaceKeys.list, queryFn: workspaceService.list })
  const me = useMeQuery()
  const list = workspaces.data
  // The saved choice, unless it was deleted or this account can't see it.
  const workspace = list?.find((w) => w.id === selected) ?? list?.[0]
  const setWorkspace = (id: ID) => { localStorage.setItem('ws.current', id); setSelected(id) }
  const currentId = workspace?.id
  useEffect(() => { if (currentId) localStorage.setItem('ws.current', currentId) }, [currentId])

  const failed = (error: Error, retry: () => void) => (
    <div className="flex min-h-dvh flex-col items-center justify-center">
      <ErrorState error={error} retry={retry} />
      <Button variant="ghost" onClick={signOut}>Sign out</Button>
    </div>
  )
  if (me.isError && !me.data) return failed(me.error, () => me.refetch())
  if (workspaces.isError && !list) return failed(workspaces.error, () => workspaces.refetch())
  if (!list || !me.data) return <Loading />
  if (!workspace) return <NoWorkspace onCreated={setWorkspace} signOut={signOut} />
  // Moving between two projects keeps the same page; switching section animates.
  const section = location.pathname.split('/').slice(0, 3).join('/')

  return (
    <WorkspaceContext.Provider value={{ workspace, workspaces: list, setWorkspace }}>
      <Shell signOut={signOut}>
        <AnimatePresence mode="wait" initial={false}>
          <motion.div key={`${workspace.id}:${section}`} initial="hidden" animate="show" exit="exit" variants={MOTION.page}
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
  const session = useSession()
  const navigate = useNavigate()
  const toast = useToast()
  const { pathname } = useLocation()
  const signOut = () => {
    authApi.logout().catch(() => toast('Signed out here, but the server couldn’t end the session.', 'error'))
    navigate('/login')
  }
  // Reached from an emailed link, signed in or not. Handled first so it stays mounted if the session ends.
  if (pathname === '/reset-password') return <ResetPassword />
  if (session) {
    if (AUTH_PATHS.includes(pathname)) return <Navigate to="/" replace />
    return <Workspace signOut={signOut} />
  }
  return (
    <Routes>
      <Route path="/login" element={<Login />} />
      <Route path="/register" element={<Register />} />
      <Route path="/reset" element={<ForgotPassword />} />
      <Route path="/two-factor" element={<TwoFactor />} />
      <Route path="*" element={<Navigate to="/login" replace />} />
    </Routes>
  )
}
