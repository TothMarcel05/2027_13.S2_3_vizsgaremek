using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PROTOTYPE_backend.DTOs.Workspace;

public record CreateWorkspaceDto(
    [Required] string WorkspaceName,
    string? Description
);

public record UsersWorkspaceListResponseDto(
    Guid WorkspaceId,
    string WorkspaceName,
    Guid OwnerId
);

public record WorkspaceDataResponseDto(
    Guid WorkspaceId,
    string WorkspaceName,
    Guid OwnerId,
    string OwnerName,
    ulong Storage
);

public record WorkspaceDataChangeDto(
    [Required] string WorkspaceName,
    string? Description
);

public record WorkspaceDataChangeResponseDto(
    Guid WorkspaceId,
    string WorkspaceName,
    string? Description
);

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

public record WorkspaceProjectResponseDto(
    Guid WorkspaceId,
    Guid ProjectId,
    string ProjectName,
    sbyte Status,
    string? Description,
    DateOnly? Deadline
);

public record WorkspaceMembersResponseDto(
    Guid WorkspaceId,
    Guid MemberId,
    string MemberName,
    Guid Role,
    DateTime? JoinedAt
);

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

public record CreateProjectDto(
    [Required] string Name,
    string? Description,
    DateTime? Deadline,
    Guid? OwnerId
);