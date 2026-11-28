/**
 * Module 2: workspaces, members, roles and outgoing invitations. The screens see one shape;
 * `workspacesApi.ts` talks to the .NET backend and `workspacesMock.ts` to the demo mock.
 */
import { USE_MOCK } from './client'
import { apiWorkspaceService } from './workspacesApi'
import { mockWorkspaceService } from './workspacesMock'
import type { ID, Workspace } from './types'

/** What the signed-in user may do in one workspace. */
export interface WorkspaceAccess {
  rename: boolean
  remove: boolean
  manageMembers: boolean
  /** Reading the role list, which the server restricts too. */
  viewRoles: boolean
  manageInvitations: boolean
}

export interface RoleOption {
  id: ID
  name: string
  /** False for Owner: ownership is not handed out as a role. */
  assignable: boolean
}

export interface MemberRow {
  userId: ID
  name: string
  /** The backend's member list has no email address. */
  email: string | null
  roleId: ID
  joinedAt: string | null
  hue: number
}

export interface SentInvitation {
  id: ID
  email: string
  roleId: ID | null
  invitedById: ID | null
  /** Set only where the source already knows the name. */
  invitedByName: string | null
  sentAt: string
}

export interface WorkspaceInput {
  name: string
  description: string
}

export interface WorkspaceService {
  list: () => Promise<Workspace[]>
  create: (name: string) => Promise<{ id: ID }>
  update: (id: ID, input: WorkspaceInput) => Promise<void>
  /** Soft delete. */
  remove: (id: ID) => Promise<void>
  access: (id: ID) => Promise<WorkspaceAccess>
  members: (id: ID) => Promise<MemberRow[]>
  /** Every role, for naming and choosing. Needs `viewRoles`. */
  roles: (id: ID) => Promise<RoleOption[]>
  setRole: (id: ID, userId: ID, roleId: ID) => Promise<void>
  removeMember: (id: ID, userId: ID) => Promise<void>
  invitations: (id: ID) => Promise<SentInvitation[]>
  invite: (id: ID, email: string, roleId: ID) => Promise<void>
  cancelInvitation: (id: ID, invitationId: ID) => Promise<void>
}

export const workspaceService: WorkspaceService = USE_MOCK ? mockWorkspaceService : apiWorkspaceService

export const workspaceKeys = {
  list: ['workspaces'] as const,
  scope: (id: ID) => ['workspace', id] as const,
  access: (id: ID) => ['workspace', id, 'access'] as const,
  members: (id: ID) => ['workspace', id, 'members'] as const,
  roles: (id: ID) => ['workspace', id, 'roles'] as const,
  invitations: (id: ID) => ['workspace', id, 'invitations'] as const,
}
