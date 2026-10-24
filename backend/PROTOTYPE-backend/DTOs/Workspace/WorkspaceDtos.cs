using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PROTOTYPE_backend.DTOs.Workspace;


public record CreateWorkspaceDto(
    [Required] Guid UserId,
    [Required] string WorkspaceName
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
    string? Descritpion,
    DateTime? Deadline
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