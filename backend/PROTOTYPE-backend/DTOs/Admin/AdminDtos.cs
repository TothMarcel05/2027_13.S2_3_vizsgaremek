using System;
using System.ComponentModel.DataAnnotations;

namespace PROTOTYPE_backend.DTOs.Admin
{
    public record UserAdminViewDto(
        Guid Id,
        string Name,
        string Email,
        sbyte GlobalRole,
        bool Is2FaEnabled,
        DateTime CreatedAt,
        DateTime? DeletedAt
    );

    public record UpdateUserRoleDto(
        [Required] Guid UserId,
        [Required] sbyte NewGlobalRole 
    );

    public record SystemOverviewStatsDto(
        int TotalUsers,
        int ActiveUsers,
        int TotalWorkspaces,
        int TotalProjects,
        int TotalTasks
    );
}