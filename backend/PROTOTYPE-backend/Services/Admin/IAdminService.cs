using System.Collections.Generic;
using System.Threading.Tasks;
using PROTOTYPE_backend.DTOs.Admin;

namespace PROTOTYPE_backend.Services.Admin
{
    public interface IAdminService
    {
        Task<List<UserRoleResponse>> GetRolesAndPermissionsAsync();
        Task<IEnumerable<UserAdminViewDto>> GetAllUsersAsync();
        Task<bool> UpdateUserRoleAsync(UpdateUserRoleDto dto);
        Task<bool> UpdateUserGlobalRoleAsync(UpdateUserGlobalRoleDto dto);
        Task<SystemOverviewStatsDto> GetSystemStatsAsync();
        Task<bool> ToggleUserStatusAsync(string userId);
        Task<bool> RemoveUserAsync(Guid userId);
    }
}