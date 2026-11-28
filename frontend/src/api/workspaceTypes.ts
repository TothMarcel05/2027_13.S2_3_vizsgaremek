/**
 * Mirrors backend/PROTOTYPE-backend/DTOs/Workspace/WorkspaceDtos.cs, DTOs/Invitations/InvitationDtos.cs,
 * DTOs/Admin/AdminDtos.cs (UserRoleResponseDto) and DTOs/Auth/AuthDtos.cs (WorkspaceTokenDto),
 * as serialised by ASP.NET Core (camelCase).
 */

/** AppInvitation.Type */
export const INVITATION_TYPE = { workspace: 1, project: 2 } as const

export interface CreateWorkspaceDto {
  workspaceName: string
  /** Required by the record; the server takes the owner from the token. */
  userId: string
  description: string | null
}

export interface WorkspaceDataChangeDto {
  workspaceName: string
  /** Replaces the stored description, so null clears it. */
  description: string | null
}

export interface WorkspaceMemberRoleChangeDto {
  roleId: string
}

export interface WorkspaceResponseDto {
  workspaceId: string
  workspaceName: string
  ownerId: string
}

export interface WorkspaceDataResponseDto extends WorkspaceResponseDto {
  ownerName: string
  /** Storage quota in bytes. */
  storage: number
}

export interface WorkspaceMemberResponseDto {
  workspaceId: string
  memberId: string
  memberName: string
  /** The role's id (app_role). */
  role: string
  joinedAt: string | null
}

export interface WorkspaceTokenDto {
  workspaceToken: string
}

export interface UserRoleResponseDto {
  roleId: string
  roleName: string
  permissions: string[]
}

export interface CreateInvitationDto {
  type: number
  targetId: string
  invitedEmail: string
  roleId: string | null
}

export interface InvitationResponseDto {
  id: string
  type: number
  targetId: string
  invitedEmail: string
  invitedUserId: string | null
  roleId: string | null
  invitedBy: string
  /** 1 = pending, 2 = accepted, 3 = refused, 4 = expired */
  status: number
  token: string
  expiresAt: string
  createdAt: string
  respondedAt: string | null
}
