/**
 * The single door to the backend. Every service goes through here.
 * With VITE_USE_MOCK=false requests hit `${VITE_API_URL}/...`; otherwise the
 * in-browser mock answers the same method + path.
 */
import type { AxiosRequestConfig } from 'axios'
import { createAuthClient, type Lock } from './authClient'
import { createSessionStore, SESSION_KEY } from './session'

export { ApiError } from './errors'

export const API_BASE: string = import.meta.env.VITE_API_URL ?? '/api'
export const USE_MOCK = import.meta.env.VITE_USE_MOCK !== 'false'

const store = createSessionStore(window.localStorage)
export const sessionStore = store
window.addEventListener('storage', (event) => {
  if (event.key === SESSION_KEY || event.key === null) store.notify()
})
localStorage.removeItem('ws.token') // left over from the earlier single-token format

/** Web Locks coordinate tabs. Without them the shared in-flight promise still covers one tab. */
const lock: Lock = <T>(task: () => Promise<T>): Promise<T> =>
  'locks' in navigator ? (navigator.locks.request('ws.auth', task) as Promise<T>) : task()

const auth = createAuthClient({ baseURL: API_BASE, store, lock })

type Method = 'GET' | 'POST' | 'PATCH' | 'PUT' | 'DELETE'
export type Query = Record<string, string | number | undefined | null>

interface RequestOptions {
  method: Method
  path: string
  body?: unknown
  query?: Query
  /** Public endpoint: send no token and never refresh on 401. */
  anonymous?: boolean
  /** Send the body as a bare JSON string, for `[FromBody] string` parameters. */
  jsonString?: boolean
  blob?: boolean
}

function withQuery(path: string, query?: Query) {
  const params = new URLSearchParams()
  Object.entries(query ?? {}).forEach(([k, v]) => {
    if (v !== undefined && v !== null && v !== '') params.set(k, String(v))
  })
  const s = params.toString()
  return s ? `${path}?${s}` : path
}

export async function request<T>({ method, path, body, query, anonymous, jsonString, blob }: RequestOptions): Promise<T> {
  const url = withQuery(path, query)
  if (USE_MOCK) {
    const { handleMock } = await import('./mock/server')
    return handleMock(method, url, body) as Promise<T>
  }
  const config: AxiosRequestConfig = { method, url, anonymous, data: body, responseType: blob ? 'blob' : 'json' }
  if (jsonString) {
    // Encode here: axios would send a string like "12345678" unquoted, which parses as a number.
    config.data = JSON.stringify(body)
    config.headers = { 'Content-Type': 'application/json' }
  }
  const response = await auth.client.request<T>(config)
  return (response.status === 204 ? undefined : response.data) as T
}

export async function logoutSession(): Promise<void> {
  if (USE_MOCK) {
    store.clear('signed-out')
    return
  }
  await auth.logout()
}

export const http = {
  get: <T>(path: string, query?: Query) => request<T>({ method: 'GET', path, query }),
  post: <T>(path: string, body?: unknown) => request<T>({ method: 'POST', path, body: body ?? {} }),
  patch: <T>(path: string, body?: unknown) => request<T>({ method: 'PATCH', path, body: body ?? {} }),
  put: <T>(path: string, body?: unknown) => request<T>({ method: 'PUT', path, body: body ?? {} }),
  delete: <T>(path: string) => request<T>({ method: 'DELETE', path }),
  blob: (path: string) => request<Blob>({ method: 'GET', path, blob: true }),
}
