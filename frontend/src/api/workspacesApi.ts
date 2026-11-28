/** WorkspaceController, InvitationController and `GET /api/roles` (AdminController). */
import { request, sessionStore, type Query, type RequestOptions } from './client'
import { getWorkspaceToken, workspaceRequest } from './workspaceToken'
import { SAMPLE_STORAGE } from './sample'
import { hueFrom } from '../lib/utils'
import * as D from './workspaceTypes'
import type { WorkspaceService } from './workspaces'

/** The largest page the list endpoints accept. */
const PAGE_SIZE = 100
const OWNER_ROLE = 'Owner'

async function allPages<T>(fetchPage: (query: Query) => Promise<T[]>): Promise<T[]> {
  const items: T[] = []
  for (let page = 1; ; page++) {
    const batch = await fetchPage({ page, limit: PAGE_SIZE })
    items.push(...batch)
    if (batch.length < PAGE_SIZE) return items
  }
}

/** The database stores UTC, but the API sends it without a zone, which browsers read as local time. */
const utc = (value: string) => (/(Z|[+-]\d\d:\d\d)$/i.test(value) ? value : `${value}Z`)

const byName = <T extends { name: string }>(a: T, b: T) => a.name.localeCompare(b.name)

const scoped = <T>(id: string, options: RequestOptions) => workspaceRequest<T>(id, options)

export const apiWorkspaceService: WorkspaceService = {
  async list() {
    const rows = await allPages((query) => request<D.WorkspaceResponseDto[]>({ method: 'GET', path: '/workspaces/my', query }))
    // The endpoint has no ordering of its own.
    return rows
      .map((w) => ({
        id: w.workspaceId, name: w.workspaceName, ownerId: w.ownerId, hue: hueFrom(w.workspaceId),
        // The backend has no plans, and the storage screens still show sample figures.
        plan: '', ...SAMPLE_STORAGE,
      }))
      .sort(byName)
  },

  async create(name) {
    const userId = sessionStore.get()?.user.id ?? ''
    const body: D.CreateWorkspaceDto = { workspaceName: name, userId, description: null }
    const created = await request<D.WorkspaceDataResponseDto>({ method: 'POST', path: '/workspaces', body })
    return { id: created.workspaceId }
  },

  async update(id, { name, description }) {
    const body: D.WorkspaceDataChangeDto = { workspaceName: name, description: description || null }
    await scoped(id, { method: 'PUT', path: `/workspaces/${id}`, body })
  },

  remove: (id) => scoped<void>(id, { method: 'DELETE', path: `/workspaces/${id}` }),

  async access(id) {
    const { permissions, isSystemAdmin } = await getWorkspaceToken(id, true)
    const can = (permission: string) => isSystemAdmin || permissions.includes(permission)
    return {
      rename: can('workspace.manage'),
      remove: can('workspace.manage'),
      manageMembers: can('workspace.members.manage'),
      viewRoles: can('workspace.roles.manage'),
      manageInvitations: can('invitation.manage'),
    }
  },

  async members(id) {
    const rows = await allPages((query) =>
      request<D.WorkspaceMemberResponseDto[]>({ method: 'GET', path: `/workspaces/${id}/members`, query }))
    return rows
      .map((m) => ({
        userId: m.memberId, name: m.memberName, email: null, roleId: m.role,
        joinedAt: m.joinedAt && utc(m.joinedAt), hue: hueFrom(m.memberId),
      }))
      .sort(byName)
  },

  async roles(id) {
    const rows = await scoped<D.UserRoleResponseDto[]>(id, { method: 'GET', path: '/roles' })
    return rows.map((r) => ({ id: r.roleId, name: r.roleName, assignable: r.roleName !== OWNER_ROLE })).sort(byName)
  },

  async setRole(id, userId, roleId) {
    const body: D.WorkspaceMemberRoleChangeDto = { roleId }
    await scoped(id, { method: 'PUT', path: `/workspaces/${id}/members/${userId}`, body })
  },

  removeMember: (id, userId) => scoped<void>(id, { method: 'DELETE', path: `/workspaces/${id}/members/${userId}` }),

  async invitations(id) {
    const rows = await scoped<D.InvitationResponseDto[]>(id, {
      method: 'GET', path: `/invitations/target/${id}`, query: { type: D.INVITATION_TYPE.workspace },
    })
    return rows
      .map((i) => ({
        id: i.id, email: i.invitedEmail, roleId: i.roleId, invitedById: i.invitedBy, invitedByName: null,
        sentAt: utc(i.createdAt),
      }))
      .sort((a, b) => b.sentAt.localeCompare(a.sentAt))
  },

  async invite(id, email, roleId) {
    const body: D.CreateInvitationDto = { type: D.INVITATION_TYPE.workspace, targetId: id, invitedEmail: email, roleId }
    await scoped(id, { method: 'POST', path: '/invitations', body })
  },

  cancelInvitation: (id, invitationId) => scoped<void>(id, { method: 'DELETE', path: `/invitations/${invitationId}` }),
}
