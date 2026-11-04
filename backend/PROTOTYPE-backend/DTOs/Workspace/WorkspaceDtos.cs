using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PROTOTYPE_backend.DTOs.Workspace;


//POST /workspace
public record CreateWorkspaceDto(
    [Required] string WorkspaceName,
    Guid userId,
    string? Description
);

//GET /workspace/user/{userId}
public record UsersWorkspaceListResponseDto(
    Guid WorkspaceId,
    string WorkspaceName,
    Guid OwnerId
);

//GET /workspace/workspaces/{workspaceId}
public record WorkspaceDataResponseDto(
    Guid WorkspaceId,
    string WorkspaceName,
    Guid OwnerId,
    string OwnerName,
    ulong Storage
);

//GET /workspace/workspaces/{workspaceId}/projects
public record WorkspaceProjectResponseDto(
    Guid WorkspaceId,
    Guid ProjectId,
    string ProjectName,
    sbyte Status,
    string? Description,
    DateOnly? Deadline
);

//GET /workspace/workspaces/{workspaceId}/members
public record WorkspaceMembersResponseDto(
    Guid WorkspaceId,
    Guid MemberId,
    string MemberName,
    Guid Role,
    DateTime? JoinedAt
);

//POST /workspace/workspaces/{workspaceId}/project
public record CreateProjectDto(
    [Required] string Name,
    string? Description,
    DateOnly? Deadline,
    Guid? OwnerId
);

//POST /workspace/workspaces/{workspaceId}/switch
public record SwitchWorkspaceDto(
    [Required] Guid WorkspaceId
);

//PUT /workspace/workspaces/{workspaceId}
public record WorkspaceDataChangeDto(
    [Required] string WorkspaceName,
    string? Description
);

//DELETE /workspace/workspaces/{workspaceId}
public record WorkspaceDataChangeResponseDto(
    Guid WorkspaceId,
    string WorkspaceName,
    string? Description
);

//PUT /workspace/workspaces/{workspaceId}/owner
public record WorkspaceOwnerChangeDto(
    [Required] Guid NewOwnerId
);

public record WorkspaceOwnerChangeResponseDto(
    Guid WorkspaceId,
    string WorkspaceName,
    Guid OwnerId,
    string OwnerName,
    Guid RoleId
);

//PUT /workspace/workspaces/{workspaceId}/members/{memberId}/role
public record WorkspaceMemberRoleChangeDto(
    [Required] Guid RoleId
);

public record WorkspaceMemberRoleChangeResponseDto(
    Guid WorkspaceId,
    string WorkspaceName,
    Guid MemberId,
    string Member,
    Guid RoleId
);

//DELETE /workspace/workspaces/{workspaceId}/members/{memberId}

//GET /workspace/workspaces/{id}/storage/usage
public record WorkspaceStorageUsageResponseDto(
    Guid WorkspaceId,
    string WorkspaceName,
    ulong StorageUsed
);


//Other DTOs
public record SelectWorkspaceDto(
    [Required(ErrorMessage = "Munkaterület azonosítója kötelező!")] Guid WorkspaceId
);

public record SelectWorkspaceResponseDto(
    string AccessToken,
    Guid WorkspaceId,
    string WorkspaceName,
    List<string> Roles,
    List<string> Permissions
);

public record UserWorkspaceRoleDto(
    Guid WorkspaceId,
    string Role
);

