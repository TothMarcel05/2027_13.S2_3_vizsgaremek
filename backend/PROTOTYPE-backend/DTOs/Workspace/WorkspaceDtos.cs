using System;
using System.ComponentModel.DataAnnotations;

namespace PROTOTYPE_backend.DTOs.Workspace;

#region Request DTOs

public record CreateWorkspaceDto(
    [Required] string workspaceName,
    Guid userId,
    string? description
);

public record WorkspaceDataChangeDto(
    [Required] string workspaceName,
    string? description
);

public record WorkspaceOwnerChangeDto(
    [Required] Guid newOwnerId
);

public record SwitchWorkspaceDto(
    [Required] Guid workspaceId
);

public record WorkspaceMemberRoleChangeDto(
    [Required] Guid roleId
);

public record CreateProjectDto(
    [Required] string name,
    string? description,
    DateOnly? deadline,
    Guid? ownerId
);

#endregion

#region Response DTOs

public record WorkspaceResponseDto(
    Guid workspaceId,
    string workspaceName,
    Guid ownerId
);

public record WorkspaceDataResponseDto(
    Guid workspaceId,
    string workspaceName,
    Guid ownerId,
    string ownerName,
    ulong storage
);

public record WorkspaceDataChangeResponseDto(
    Guid workspaceId,
    string workspaceName,
    string? description
);

public record WorkspaceOwnerChangeResponseDto(
    Guid workspaceId,
    string workspaceName,
    Guid ownerId,
    string ownerName,
    Guid roleId
);

public record WorkspaceStorageUsageResponseDto(
    Guid workspaceId,
    string workspaceName,
    ulong storageUsed
);

public record WorkspaceMemberResponseDto(
    Guid workspaceId,
    Guid memberId,
    string memberName,
    Guid role,
    DateTime? joinedAt
);

public record WorkspaceMemberRoleChangeResponseDto(
    Guid workspaceId,
    string workspaceName,
    Guid memberId,
    string member,
    Guid roleId
);

public record WorkspaceProjectResponseDto(
    Guid workspaceId,
    Guid projectId,
    string projectName,
    sbyte status,
    string? description,
    DateOnly? deadline
);

#endregion