using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PROTOTYPE_backend.DTOs.Workspace;

#region Request DTOs

/// <summary>
/// Új munkaterület létrehozására szolgáló kérés.
/// </summary>
public record CreateWorkspaceDto(
    [Required] string workspaceName,
    Guid userId,
    string? description
);

/// <summary>
/// Munkaterület adatainak módosítására szolgáló kérés.
/// </summary>
public record WorkspaceDataChangeDto(
    [Required] string workspaceName,
    string? description
);

/// <summary>
/// Tulajdonjog átruházására szolgáló kérés.
/// </summary>
public record WorkspaceOwnerChangeDto(
    [Required] Guid newOwnerId
);

/// <summary>
/// Munkaterület váltásra szolgáló kérés.
/// </summary>
public record SwitchWorkspaceDto(
    [Required] Guid workspaceId
);

/// <summary>
/// Tag szerepkörének módosítására szolgáló kérés.
/// </summary>
public record WorkspaceMemberRoleChangeDto(
    [Required] Guid roleId
);

/// <summary>
/// Új projekt létrehozására szolgáló kérés a munkaterületen belül.
/// </summary>
public record CreateProjectDto(
    [Required] string name,
    string? description,
    DateOnly? deadline,
    Guid? ownerId
);

#endregion

#region Response DTOs

/// <summary>
/// Felhasználó munkaterületeinek listázásakor visszakapott elem.
/// </summary>
public record UsersWorkspaceListResponseDto(
    Guid workspaceId,
    string workspaceName,
    Guid ownerId
);

/// <summary>
/// Munkaterület részletes adatait tartalmazó válasz.
/// </summary>
public record WorkspaceDataResponseDto(
    Guid workspaceId,
    string workspaceName,
    Guid ownerId,
    string ownerName,
    ulong storage
);

/// <summary>
/// Munkaterület adatmódosításának válasz DTO-ja.
/// </summary>
public record WorkspaceDataChangeResponseDto(
    Guid workspaceId,
    string workspaceName,
    string? description
);

/// <summary>
/// Tulajdonosváltás eredményének válasz DTO-ja.
/// </summary>
public record WorkspaceOwnerChangeResponseDto(
    Guid workspaceId,
    string workspaceName,
    Guid ownerId,
    string ownerName,
    Guid roleId
);

/// <summary>
/// Tárhelyhasználati statisztikát tartalmazó válasz.
/// </summary>
public record WorkspaceStorageUsageResponseDto(
    Guid workspaceId,
    string workspaceName,
    ulong storageUsed
);

/// <summary>
/// Munkaterület tagjának adatait és szerepkörét leíró válasz.
/// </summary>
public record WorkspaceMembersResponseDto(
    Guid workspaceId,
    Guid memberId,
    string memberName,
    Guid role,
    DateTime? joinedAt
);

/// <summary>
/// Tag szerepkörmódosításának válasz DTO-ja.
/// </summary>
public record WorkspaceMemberRoleChangeResponseDto(
    Guid workspaceId,
    string workspaceName,
    Guid memberId,
    string member,
    Guid roleId
);

/// <summary>
/// Munkaterülethez tartozó projektek listázásakor használt válasz DTO.
/// </summary>
public record WorkspaceProjectResponseDto(
    Guid workspaceId,
    Guid projectId,
    string projectName,
    sbyte status,
    string? description,
    DateOnly? deadline
);

#endregion