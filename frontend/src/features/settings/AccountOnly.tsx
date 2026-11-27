import { Navigate, Route, Routes } from 'react-router'
import { LogOut } from 'lucide-react'
import { Brand } from '../../components/layout/shared'
import { Button } from '../../components/ui/core'
import { Profile } from './Profile'

/**
 * Signed-in frame for the real API while the workspace modules are not yet connected.
 * The full app shell needs workspace data, so this one offers only the account pages.
 */
export function AccountOnly({ signOut }: { signOut: () => void }) {
  return (
    <div className="flex min-h-dvh flex-col">
      <header className="topbar">
        <Brand />
        <div className="ml-auto flex items-center gap-2">
          <Button variant="outline" size="sm" icon={<LogOut size={15} />} onClick={signOut}>Sign out</Button>
        </div>
      </header>
      <main className="app-main mx-auto w-full max-w-3xl">
        <p className="mb-4 text-sm text-muted">Account settings are connected to the server. The workspace pages aren’t connected yet.</p>
        <Routes>
          <Route path="/profile" element={<Profile />} />
          <Route path="*" element={<Navigate to="/profile" replace />} />
        </Routes>
      </main>
    </div>
  )
}
