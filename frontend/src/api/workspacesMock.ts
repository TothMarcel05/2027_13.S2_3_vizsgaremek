/** Demo-mode stand-in for Module 2, built on the in-browser mock's routes. Roles are fixed here. */
import { http } from './client'
import { workspaceApi } from './services'
import type { Invitation, Workspace, WorkspaceRole } from './types'
import type { RoleOption, WorkspaceService } from './workspaces'

const ROLES: RoleOption[] = [
  { id: 'owner', name: 'Owner', assignable: false }, { id: 'admin', name: 'Admin', assignable: true },
  { id: 'member', name: 'Member', assignable: true }, { id: 'viewer', name: 'Viewer', assignable: true },
]
const asRole = (id: string) => id as WorkspaceRole

export const mockWorkspaceService: WorkspaceService = {
  list: () => http.get<Workspace[]>('/workspaces'),
  create: (name) => http.post<Workspace>('/workspaces', { name }),
  async update(id, { name }) { await http.patch(`/workspaces/${id}`, { name }) },
  remove: (id) => http.delete<void>(`/workspaces/${id}`),

  async access(id) {
    const [members, me] = await Promise.all([workspaceApi.members(id), http.get<{ userId: string }>('/users/me')])
    const role = members.find((m) => m.userId === me.userId)?.role
    const owner = role === 'owner'
    const manager = owner || role === 'admin'
    return { rename: manager, remove: owner, manageMembers: manager, viewRoles: true, manageInvitations: manager }
  },

  async members(id) {
    const rows = await workspaceApi.members(id)
    return rows.map((m) => ({
      userId: m.userId, name: m.user.name, email: m.user.email, roleId: m.role, joinedAt: m.joinedAt, hue: m.user.hue,
    }))
  },

  roles: async () => ROLES,
  setRole: (id, userId, roleId) => http.patch<void>(`/workspaces/${id}/members/${userId}`, { role: asRole(roleId) }),
  removeMember: (id, userId) => http.delete<void>(`/workspaces/${id}/members/${userId}`),

  async invitations(id) {
    const rows = await http.get<Invitation[]>(`/workspaces/${id}/invitations`)
    return rows.map((i) => ({
      id: i.id, email: i.email, roleId: i.role, invitedById: null, invitedByName: i.invitedBy, sentAt: i.sentAt,
    }))
  },

  async invite(id, email, roleId) { await http.post(`/workspaces/${id}/invitations`, { email, role: asRole(roleId) }) },
  cancelInvitation: (_id, invitationId) => http.delete<void>(`/invitations/${invitationId}`),
}
