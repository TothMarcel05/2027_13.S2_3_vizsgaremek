/**
 * The signed-in session. localStorage is the single source of truth, so every tab sees
 * the same tokens. This matters because the backend rotates the refresh token on
 * each use: a tab holding a stale copy would be rejected.
 *
 * Trade-off: the backend returns tokens in the response body (no httpOnly cookie), so they are
 * readable by any script running on this origin.
 */
import type { AuthResponseDto, UserDto } from './authTypes'

export interface Session {
  accessToken: string
  refreshToken: string
  user: UserDto
}

/** Why the last session ended, so the sign-in screen can explain it. */
export type SessionEnd = 'signed-out' | 'expired'

export interface SessionStore {
  get: () => Session | null
  set: (auth: AuthResponseDto) => void
  clear: (reason: SessionEnd) => void
  endReason: () => SessionEnd | null
  subscribe: (listener: () => void) => () => void
}

type KeyValueStorage = Pick<Storage, 'getItem' | 'setItem' | 'removeItem'>

export const SESSION_KEY = 'ws.session'

function isSession(value: unknown): value is Session {
  if (typeof value !== 'object' || value === null) return false
  const s = value as Partial<Record<keyof Session, unknown>>
  return typeof s.accessToken === 'string' && typeof s.refreshToken === 'string'
    && typeof s.user === 'object' && s.user !== null && typeof (s.user as Partial<UserDto>).id === 'string'
}

export function createSessionStore(storage: KeyValueStorage): SessionStore & { notify: () => void } {
  const listeners = new Set<() => void>()
  let raw: string | null = null
  let cached: Session | null = null
  let reason: SessionEnd | null = null
  const notify = () => listeners.forEach((listener) => listener())

  return {
    // Returns a stable reference while storage is unchanged (required by useSyncExternalStore).
    get() {
      const next = storage.getItem(SESSION_KEY)
      if (next !== raw) {
        raw = next
        cached = null
        if (next) {
          try {
            const parsed: unknown = JSON.parse(next)
            if (isSession(parsed)) cached = parsed
          } catch { /* corrupt entry: treat as signed out */ }
        }
      }
      return cached
    },
    set({ accessToken, refreshToken, user }) {
      reason = null
      storage.setItem(SESSION_KEY, JSON.stringify({ accessToken, refreshToken, user }))
      notify()
    },
    clear(why) {
      reason = why
      storage.removeItem(SESSION_KEY)
      notify()
    },
    endReason: () => reason,
    subscribe(listener) {
      listeners.add(listener)
      return () => { listeners.delete(listener) }
    },
    notify,
  }
}
