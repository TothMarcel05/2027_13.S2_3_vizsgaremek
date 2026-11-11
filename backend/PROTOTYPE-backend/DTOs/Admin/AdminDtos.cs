using System;
using System.ComponentModel.DataAnnotations;

namespace PROTOTYPE_backend.DTOs.Admin
{

    #region Request DTOs
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
        [Required] Guid NewRole 
    );


    public record UpdateUserGlobalRoleDto(
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

    #endregion


    #region Response DTOs
    public record UserRoleResponse
    (
        Guid roleId,
        string roleName,
        List<string> permission
    );
    #endregion
}