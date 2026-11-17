using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PROTOTYPE_backend.DTOs.Admin;

#region Request DTOs

public record AdminCreateRoleDto(
    [Required] string Name,
    List<string> Permissions
);

public record UpdateRolePermissionsDto(
    [Required] List<string> Permissions
);

public record UpdateUserGlobalRoleDto(
    [Required] sbyte NewGlobalRole
);

#endregion

#region Response DTOs

public record UserAdminViewDto(
    Guid Id,
    string Name,
    string Email,
    sbyte GlobalRole,
    bool Is2FaEnabled,
    DateTime CreatedAt,
    DateTime? DeletedAt
);

public record SystemOverviewStatsDto(
    int TotalUsers,
    int ActiveUsers,
    int TotalWorkspaces,
    int TotalProjects,
    int TotalTasks
);

public record UserRoleResponseDto(
    Guid RoleId,
    string RoleName,
    List<string> Permissions 
);

#endregion