/**
 * The single door to the backend. Every service goes through `http`.
 * With VITE_USE_MOCK=false requests hit `${VITE_API_URL}/...`; otherwise the
 * in-browser mock answers the exact same method + path contract.
 */
export const API_BASE: string = import.meta.env.VITE_API_URL ?? '/api'
export const USE_MOCK = import.meta.env.VITE_USE_MOCK !== 'false'

const TOKEN_KEY = 'ws.token'
export const tokenStore = {
  get: () => localStorage.getItem(TOKEN_KEY),
  set: (t: string) => localStorage.setItem(TOKEN_KEY, t),
  clear: () => localStorage.removeItem(TOKEN_KEY),
}

export class ApiError extends Error {
  constructor(public status: number, message: string) {
    super(message)
  }
}

type Method = 'GET' | 'POST' | 'PATCH' | 'PUT' | 'DELETE'
export type Query = Record<string, string | number | undefined | null>

function withQuery(path: string, query?: Query) {
  const params = new URLSearchParams()
  Object.entries(query ?? {}).forEach(([k, v]) => {
    if (v !== undefined && v !== null && v !== '') params.set(k, String(v))
  })
  const s = params.toString()
  return s ? `${path}?${s}` : path
}

async function send(method: Method, path: string, body?: unknown): Promise<Response> {
  const headers: Record<string, string> = {}
  const token = tokenStore.get()
  if (token) headers.Authorization = `Bearer ${token}`
  const isForm = body instanceof FormData
  if (body !== undefined && !isForm) headers['Content-Type'] = 'application/json'
  const res = await fetch(API_BASE + path, {
    method,
    headers,
    body: body === undefined ? undefined : isForm ? body : JSON.stringify(body),
  })
  if (!res.ok) {
    const data = await res.json().catch(() => null)
    throw new ApiError(res.status, data?.message ?? res.statusText)
  }
  return res
}

async function request<T>(method: Method, path: string, body?: unknown): Promise<T> {
  if (USE_MOCK) {
    const { handleMock } = await import('./mock/server')
    return handleMock(method, path, body) as Promise<T>
  }
  const res = await send(method, path, body)
  return (res.status === 204 ? undefined : await res.json()) as T
}

export const http = {
  get: <T>(path: string, query?: Query) => request<T>('GET', withQuery(path, query)),
  post: <T>(path: string, body?: unknown) => request<T>('POST', path, body ?? {}),
  patch: <T>(path: string, body?: unknown) => request<T>('PATCH', path, body ?? {}),
  put: <T>(path: string, body?: unknown) => request<T>('PUT', path, body ?? {}),
  delete: <T>(path: string) => request<T>('DELETE', path),
  async blob(path: string): Promise<Blob> {
    if (USE_MOCK) {
      const { handleMock } = await import('./mock/server')
      return handleMock('GET', path) as Promise<Blob>
    }
    return (await send('GET', path)).blob()
  },
}
