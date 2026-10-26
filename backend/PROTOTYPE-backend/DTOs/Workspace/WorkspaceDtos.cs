using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PROTOTYPE_backend.DTOs.Workspace;


public record CreateWorkspaceDto(
    [Required] string WorkspaceName,
    string Description
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

public record SelectWorkspaceDto(
    [Required(ErrorMessage = "Munkaterület azonosítója kötelező!")] Guid workspaceId
);

public record SelectWorkspaceResponseDto(
    string AccessToken,
    Guid WorkspaceId,
    string WorkspaceName,
    List<string> Roles,
    List<string> Permissions
);


public record UserWorkspaceRoleDto(
    Guid workspaceId,
    string role
);


public record CreateProjectDto(
    [Required] string Name,
    string? Description,
    DateTime? Deadline,
    Guid? OwnerId
);