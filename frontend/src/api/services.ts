import { http, USE_MOCK, API_BASE, tokenStore } from './client'
import type * as T from './types'

export const authApi = {
  login: (email: string, password: string) => http.post<T.AuthResult>('/auth/login', { email, password }),
  register: (name: string, email: string, password: string) =>
    http.post<T.AuthResult>('/auth/register', { name, email, password }),
  verify2fa: (challengeId: string, code: string, kind: 'totp' | 'email' | 'recovery') =>
    http.post<T.AuthResult>('/auth/2fa/verify', { challengeId, code, kind }),
  resendEmailCode: (challengeId: string) => http.post<void>('/auth/2fa/email/resend', { challengeId }),
  requestReset: (email: string) => http.post<void>('/auth/password/forgot', { email }),
  resetPassword: (code: string, password: string) => http.post<void>('/auth/password/reset', { code, password }),
  logout: () => http.post<void>('/auth/logout'),
}

export const meApi = {
  get: () => http.get<T.User>('/me'),
  update: (patch: Partial<T.User>) => http.patch<T.User>('/me', patch),
  changePassword: (current: string, next: string) => http.post<void>('/me/password', { current, next }),
  setupTotp: () => http.post<{ secret: string; otpauthUrl: string }>('/me/2fa/totp/setup'),
  enable2fa: (method: 'totp' | 'email', code: string) =>
    http.post<{ recoveryCodes: string[] }>('/me/2fa/enable', { method, code }),
  disable2fa: () => http.delete<void>('/me/2fa'),
  recoveryCodes: () => http.post<{ recoveryCodes: string[] }>('/me/2fa/recovery-codes'),
  sessions: () => http.get<T.Session[]>('/me/sessions'),
  revokeSession: (id: T.ID) => http.delete<void>(`/me/sessions/${id}`),
  revokeOtherSessions: () => http.delete<void>('/me/sessions'),
}

export const workspaceApi = {
  list: () => http.get<T.Workspace[]>('/workspaces'),
  create: (name: string) => http.post<T.Workspace>('/workspaces', { name }),
  update: (id: T.ID, patch: Partial<T.Workspace>) => http.patch<T.Workspace>(`/workspaces/${id}`, patch),
  dashboard: (id: T.ID) => http.get<T.DashboardData>(`/workspaces/${id}/dashboard`),
  members: (id: T.ID) => http.get<T.Member[]>(`/workspaces/${id}/members`),
  setRole: (id: T.ID, userId: T.ID, role: T.WorkspaceRole) =>
    http.patch<void>(`/workspaces/${id}/members/${userId}`, { role }),
  removeMember: (id: T.ID, userId: T.ID) => http.delete<void>(`/workspaces/${id}/members/${userId}`),
  transfer: (id: T.ID, userId: T.ID) => http.post<void>(`/workspaces/${id}/transfer-ownership`, { userId }),
  sentInvitations: (id: T.ID) => http.get<T.Invitation[]>(`/workspaces/${id}/invitations`),
  invite: (id: T.ID, email: string, role: T.WorkspaceRole) =>
    http.post<T.Invitation>(`/workspaces/${id}/invitations`, { email, role }),
  cancelInvitation: (invId: T.ID) => http.delete<void>(`/invitations/${invId}`),
  myInvitations: () => http.get<T.Invitation[]>('/invitations/pending'),
  respondInvitation: (invId: T.ID, accept: boolean) =>
    http.post<void>(`/invitations/${invId}/${accept ? 'accept' : 'decline'}`),
}

export const projectApi = {
  list: (ws: T.ID) => http.get<T.Project[]>(`/workspaces/${ws}/projects`),
  get: (id: T.ID) => http.get<T.Project>(`/projects/${id}`),
  create: (ws: T.ID, data: Partial<T.Project>) => http.post<T.Project>(`/workspaces/${ws}/projects`, data),
  update: (id: T.ID, data: Partial<T.Project>) => http.patch<T.Project>(`/projects/${id}`, data),
  remove: (id: T.ID) => http.delete<void>(`/projects/${id}`),
}

export const taskApi = {
  list: (projectId: T.ID) => http.get<T.Task[]>(`/projects/${projectId}/tasks`),
  create: (projectId: T.ID, data: Partial<T.Task>) => http.post<T.Task>(`/projects/${projectId}/tasks`, data),
  update: (id: T.ID, data: Partial<T.Task>) => http.patch<T.Task>(`/tasks/${id}`, data),
  reserve: (id: T.ID) => http.post<T.Task>(`/tasks/${id}/reserve`),
  unreserve: (id: T.ID) => http.post<T.Task>(`/tasks/${id}/unreserve`),
  remove: (id: T.ID) => http.delete<void>(`/tasks/${id}`),
}

export const chatApi = {
  channels: (ws: T.ID) => http.get<T.Channel[]>(`/workspaces/${ws}/channels`),
  messages: (channelId: T.ID) => http.get<T.Message[]>(`/channels/${channelId}/messages`),
  send: (channelId: T.ID, body: string, attachments: T.Attachment[]) =>
    http.post<T.Message>(`/channels/${channelId}/messages`, { body, attachments }),
  markRead: (channelId: T.ID) => http.post<void>(`/channels/${channelId}/read`),
  /**
   * Live updates. Mock: an in-memory event bus. Real: Server-Sent Events on
   * /api/channels/:id/stream (swap for a WebSocket here if the backend uses one).
   */
  subscribe(channelId: T.ID, onEvent: (e: { type: 'typing' | 'message'; userId: T.ID }) => void): () => void {
    let stop: (() => void) | undefined
    let cancelled = false
    if (USE_MOCK) {
      import('./mock/server').then((m) => {
        if (!cancelled) stop = m.subscribeMock(channelId, onEvent)
      })
    } else {
      const source = new EventSource(`${API_BASE}/channels/${channelId}/stream?token=${tokenStore.get() ?? ''}`)
      source.onmessage = (ev) => onEvent(JSON.parse(ev.data))
      stop = () => source.close()
    }
    return () => {
      cancelled = true
      stop?.()
    }
  },
}

export const calendarApi = {
  list: (ws: T.ID, from: string, to: string, projectId?: T.ID) =>
    http.get<T.CalendarEvent[]>(`/workspaces/${ws}/events`, { from, to, projectId }),
  create: (ws: T.ID, data: Partial<T.CalendarEvent>) => http.post<T.CalendarEvent>(`/workspaces/${ws}/events`, data),
  update: (id: T.ID, data: Partial<T.CalendarEvent>) => http.patch<T.CalendarEvent>(`/events/${id}`, data),
  remove: (id: T.ID) => http.delete<void>(`/events/${id}`),
}

export const fileApi = {
  list: (ws: T.ID) => http.get<T.FileNode[]>(`/workspaces/${ws}/files`),
  createFolder: (ws: T.ID, name: string, parentId: T.ID | null) =>
    http.post<T.FileNode>(`/workspaces/${ws}/folders`, { name, parentId }),
  upload(ws: T.ID, file: File, parentId: T.ID | null) {
    if (USE_MOCK) return http.post<T.FileNode>(`/workspaces/${ws}/files`, { file, parentId })
    const form = new FormData()
    form.append('file', file)
    if (parentId) form.append('parentId', parentId)
    return http.post<T.FileNode>(`/workspaces/${ws}/files`, form)
  },
  update: (id: T.ID, data: Partial<T.FileNode>) => http.patch<T.FileNode>(`/files/${id}`, data),
  share: (id: T.ID, shares: T.Share[]) => http.put<T.FileNode>(`/files/${id}/shares`, { shares }),
  remove: (id: T.ID) => http.delete<void>(`/files/${id}`),
  download: (id: T.ID) => http.blob(`/files/${id}/download`),
}

export const notificationApi = {
  list: () => http.get<T.Notification[]>('/notifications'),
  markRead: (id: T.ID) => http.post<void>(`/notifications/${id}/read`),
  markAllRead: () => http.post<void>('/notifications/read-all'),
}

export const trashApi = {
  list: (ws: T.ID) => http.get<T.TrashItem[]>(`/workspaces/${ws}/trash`),
  restore: (id: T.ID) => http.post<void>(`/trash/${id}/restore`),
  purge: (id: T.ID) => http.delete<void>(`/trash/${id}`),
}

export const adminApi = {
  users: (q: { search?: string; page: number; pageSize: number; role?: string }) =>
    http.get<T.Paged<T.User>>('/admin/users', q),
  updateUser: (id: T.ID, data: Partial<T.User>) => http.patch<T.User>(`/admin/users/${id}`, data),
  roles: () => http.get<{ roles: T.Role[]; permissions: T.Permission[] }>('/admin/roles'),
  saveRoles: (roles: T.Role[]) => http.put<void>('/admin/roles', { roles }),
  createRole: (name: string, description: string) => http.post<T.Role>('/admin/roles', { name, description }),
}
