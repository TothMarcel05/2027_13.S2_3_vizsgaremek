/**
 * The workspace-scoped JWT. The login token carries no permissions: every `[Authorize(Policy = ...)]`
 * endpoint needs the token from `POST /workspaces/{id}/switch`. It is kept in memory only and never
 * replaces the session token. It has no refresh token of its own, so it is simply requested again.
 */
import { request, scopedRequest, sessionStore, type RequestOptions } from './client'
import { ApiError } from './errors'
import { GLOBAL_ROLE } from './authTypes'
import type { WorkspaceTokenDto } from './workspaceTypes'

export interface WorkspaceClaims {
  permissions: string[]
  isSystemAdmin: boolean
  /** Epoch milliseconds; 0 when the token names no expiry. */
  expiresAt: number
}

export interface WorkspaceToken extends WorkspaceClaims {
  token: string
}

const ROLE_CLAIMS = ['role', 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role']
/** Ask for a new token this long before the old one runs out. */
const EXPIRY_MARGIN = 30_000

const isRecord = (value: unknown): value is Record<string, unknown> => typeof value === 'object' && value !== null

function decodePayload(token: string): Record<string, unknown> {
  const part = token.split('.')[1] ?? ''
  const binary = atob(part.replace(/-/g, '+').replace(/_/g, '/'))
  const parsed: unknown = JSON.parse(new TextDecoder().decode(Uint8Array.from(binary, (c) => c.charCodeAt(0))))
  if (!isRecord(parsed)) throw new Error('Not a token payload')
  return parsed
}

/**
 * Reads what the server put in the token. Used only to decide which controls to show;
 * the server checks the signature and enforces the permissions itself.
 */
export function readClaims(token: string, workspaceId: string): WorkspaceClaims {
  try {
    const payload = decodePayload(token)
    const raw = payload.all_roles_permissions
    const map: unknown = typeof raw === 'string' ? JSON.parse(raw) : {}
    const list: unknown = isRecord(map) ? map[workspaceId.toLowerCase()] : undefined
    return {
      permissions: Array.isArray(list) ? list.filter((p): p is string => typeof p === 'string') : [],
      isSystemAdmin: ROLE_CLAIMS.some((claim) => String(payload[claim]) === String(GLOBAL_ROLE.systemAdmin)),
      expiresAt: typeof payload.exp === 'number' ? payload.exp * 1000 : 0,
    }
  } catch {
    throw new ApiError(0, 'The server sent an unreadable workspace token.')
  }
}

const cache = new Map<string, WorkspaceToken>()
const inFlight = new Map<string, Promise<WorkspaceToken>>()

/** `fresh` skips the cache, for when roles may have changed. */
export function getWorkspaceToken(workspaceId: string, fresh = false): Promise<WorkspaceToken> {
  // Keyed by account, so a token can never be reused after someone else signs in.
  const key = `${sessionStore.get()?.user.id}:${workspaceId}`
  const cached = cache.get(key)
  if (!fresh && cached && cached.expiresAt - EXPIRY_MARGIN > Date.now()) return Promise.resolve(cached)

  let pending = inFlight.get(key)
  if (!pending) {
    pending = request<WorkspaceTokenDto>({ method: 'POST', path: `/workspaces/${workspaceId}/switch` })
      .then(({ workspaceToken }) => {
        const result = { token: workspaceToken, ...readClaims(workspaceToken, workspaceId) }
        cache.set(key, result)
        return result
      })
      .finally(() => inFlight.delete(key))
    inFlight.set(key, pending)
  }
  return pending
}

/**
 * Calls a permission-protected endpoint. A 401 (expired) or 403 (permissions changed since the token
 * was issued) is tried once more with a new token. Neither status means the server did anything.
 */
export async function workspaceRequest<T>(workspaceId: string, options: RequestOptions): Promise<T> {
  const { token } = await getWorkspaceToken(workspaceId)
  try {
    return await scopedRequest<T>(options, token)
  } catch (error) {
    if (!(error instanceof ApiError) || (error.status !== 401 && error.status !== 403)) throw error
    const renewed = await getWorkspaceToken(workspaceId, true)
    return scopedRequest<T>(options, renewed.token)
  }
}
