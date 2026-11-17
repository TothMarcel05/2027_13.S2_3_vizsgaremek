using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PROTOTYPE_backend.DTOs.Admin;

namespace PROTOTYPE_backend.Services.Admin;

public interface IAdminService
{
    Task<List<UserRoleResponseDto>> GetRolesAndPermissionsAsync();
    Task<UserRoleResponseDto> CreateRoleAsync(AdminCreateRoleDto dto);
    Task<bool> UpdateRolePermissionsAsync(Guid roleId, UpdateRolePermissionsDto dto);

    Task<IEnumerable<UserAdminViewDto>> GetAllUsersAsync(int page = 1, string? search = null);
    Task<bool> UpdateUserGlobalRoleAsync(Guid userId, UpdateUserGlobalRoleDto dto);
    Task<bool> ToggleUserStatusAsync(Guid userId); 
    Task<bool> RemoveUserAsync(Guid userId);

    Task<SystemOverviewStatsDto> GetSystemStatsAsync();
}