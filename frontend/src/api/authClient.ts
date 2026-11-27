/**
 * The axios instance and its token handling. Kept free of Vite and DOM globals so the refresh
 * behaviour can be tested in isolation.
 *
 * - Every request carries the access token unless it is marked `anonymous`.
 * - A 401 triggers one shared refresh; requests failing meanwhile wait for it and are then retried.
 * - A request is retried at most once, and the refresh call never goes through these interceptors,
 *   so refreshing cannot loop.
 * - Several endpoints also answer 401 for a wrong password. The status and code match an expired
 *   token, so the retry settles it: if the fresh token still gets 401, the error is passed to the
 *   caller and the session is kept. Only a rejected refresh token ends the session.
 */
import axios, { AxiosError, type AxiosAdapter, type AxiosInstance } from 'axios'
import type { AuthResponseDto } from './authTypes'
import type { SessionStore } from './session'
import { ApiError, fallbackMessage, parseErrorBody } from './errors'

declare module 'axios' {
  export interface AxiosRequestConfig {
    /** Public endpoint: send no token and never refresh on 401. */
    anonymous?: boolean
  }
  export interface InternalAxiosRequestConfig {
    anonymous?: boolean
    retried?: boolean
    sentToken?: string
    sessionUserId?: string
  }
}

/** Runs a task while holding a lock, so that only one refresh happens at a time. */
export type Lock = <T>(task: () => Promise<T>) => Promise<T>

interface Options {
  baseURL: string
  store: SessionStore
  lock: Lock
  adapter?: AxiosAdapter
}

export function toApiError(error: unknown): ApiError {
  if (error instanceof ApiError) return error
  if (error instanceof AxiosError) {
    const status = error.response?.status ?? 0
    const body = parseErrorBody(error.response?.data)
    return new ApiError(status, body?.message ?? fallbackMessage(status), body?.code)
  }
  return new ApiError(0, error instanceof Error ? error.message : fallbackMessage(0))
}

const sessionEnded = () => new ApiError(401, 'Your session has ended. Sign in again.', 'NINCS_HITELESITVE')

export function createAuthClient({ baseURL, store, lock, adapter }: Options) {
  const base = { baseURL, timeout: 30_000, ...(adapter ? { adapter } : {}) }
  const client: AxiosInstance = axios.create(base)
  const bare: AxiosInstance = axios.create(base) // no interceptors: used by refresh and logout
  let inFlight: Promise<string> | null = null

  const requestRefresh = async (refreshToken: string) =>
    (await bare.post<AuthResponseDto>('/auth/refresh', { refreshToken })).data

  /** Resolves to a usable access token that differs from `staleToken`. */
  function refreshAccessToken(staleToken: string): Promise<string> {
    inFlight ??= lock(async () => {
      const session = store.get()
      if (!session) throw sessionEnded()
      // Another tab, or an earlier refresh, already replaced the token that failed.
      if (session.accessToken !== staleToken) return session.accessToken

      try {
        const auth = await requestRefresh(session.refreshToken)
        const current = store.get()
        // Signed out or signed in again while waiting: don't bring the old session back.
        if (current?.refreshToken !== session.refreshToken) {
          if (current) return current.accessToken
          throw sessionEnded()
        }
        store.set(auth)
        return auth.accessToken
      } catch (error) {
        const failure = toApiError(error)
        const rejected = failure.status === 400 || failure.status === 401
        // Network and server errors keep the session so a later request can try again.
        if (rejected && store.get()?.refreshToken === session.refreshToken) store.clear('expired')
        throw rejected ? sessionEnded() : failure
      }
    }).finally(() => { inFlight = null })
    return inFlight
  }

  client.interceptors.request.use((config) => {
    if (config.anonymous) return config
    const session = store.get()
    // A retry must never be replayed under a different account.
    if (config.retried && session?.user.id !== config.sessionUserId) throw sessionEnded()
    if (session) {
      config.headers.set('Authorization', `Bearer ${session.accessToken}`)
      config.sentToken = session.accessToken
      config.sessionUserId = session.user.id
    }
    return config
  })

  client.interceptors.response.use(undefined, async (error: unknown) => {
    if (!(error instanceof AxiosError)) throw toApiError(error)
    const { config, response } = error
    if (response?.status !== 401 || !config || config.anonymous || config.retried || !config.sentToken) {
      throw toApiError(error)
    }
    if (store.get()?.user.id !== config.sessionUserId) throw sessionEnded()
    config.retried = true
    await refreshAccessToken(config.sentToken)
    return client.request(config)
  })

  /**
   * Signs out locally at once, then revokes the refresh token on the server. The request body
   * depends on the current refresh token, so this can't use the generic retry: after a rotation
   * the old body would name a token that no longer exists and the new one would stay valid.
   */
  async function logout(): Promise<void> {
    const revoke = (accessToken: string, refreshToken: string) =>
      bare.post('/auth/logout', { refreshToken }, { headers: { Authorization: `Bearer ${accessToken}` } })

    await lock(async () => {
      const session = store.get()
      store.clear('signed-out')
      if (!session) return
      try {
        await revoke(session.accessToken, session.refreshToken)
      } catch (error) {
        if (toApiError(error).status !== 401) throw toApiError(error)
        try {
          const auth = await requestRefresh(session.refreshToken)
          await revoke(auth.accessToken, auth.refreshToken)
        } catch (retryError) {
          // A rejected refresh token means there is nothing left to revoke.
          if (toApiError(retryError).status !== 401) throw toApiError(retryError)
        }
      }
    })
  }

  return { client, logout }
}
