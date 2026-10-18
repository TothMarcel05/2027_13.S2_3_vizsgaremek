using System;
using System.ComponentModel.DataAnnotations;

namespace PROTOTYPE_backend.DTOs.Admin
{
    public record UserAdminViewDto(
        string Id,
        string Name,
        string Email,
        byte GlobalRole,
        bool Is2FaEnabled,
        DateTime CreatedAt,
        DateTime? DeletedAt
    );

    public record UpdateUserRoleDto(
        [Required] string UserId,
        [Required] byte NewGlobalRole 
    );

    public record SystemOverviewStatsDto(
        int TotalUsers,
        int ActiveUsers,
        int TotalWorkspaces,
        int TotalProjects,
        int TotalTasks
    );
}