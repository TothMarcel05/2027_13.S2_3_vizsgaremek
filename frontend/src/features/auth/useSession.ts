import { useSyncExternalStore } from 'react'
import { sessionStore } from '../../api/client'
import type { Session } from '../../api/session'

export function useSession(): Session | null {
  return useSyncExternalStore(sessionStore.subscribe, sessionStore.get)
}
