/**
 * The modules that aren't connected yet. `http` answers them from the mock in both modes; their paths
 * and shapes are assumptions to replace when each is connected. Module 2 lives in `workspaces.ts`.
 */
import { http } from './client'
import type * as T from './types'

/** Sample data only. `members` feeds the names and avatars on the sample screens. */
export const workspaceApi = {
  dashboard: (id: T.ID) => http.get<T.DashboardData>(`/workspaces/${id}/dashboard`),
  members: (id: T.ID) => http.get<T.Member[]>(`/workspaces/${id}/members`),
  /** Demo mode only. */
  transfer: (id: T.ID, userId: T.ID) => http.post<void>(`/workspaces/${id}/transfer-ownership`, { userId }),
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
  /** Live updates from the mock's in-memory event bus. The backend will use SignalR. */
  subscribe(channelId: T.ID, onEvent: (e: { type: 'typing' | 'message'; userId: T.ID }) => void): () => void {
    let stop: (() => void) | undefined
    let cancelled = false
    import('./mock/server').then((m) => {
      if (!cancelled) stop = m.subscribeMock(channelId, onEvent)
    })
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
  upload: (ws: T.ID, file: File, parentId: T.ID | null) =>
    http.post<T.FileNode>(`/workspaces/${ws}/files`, { file, parentId }),
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
