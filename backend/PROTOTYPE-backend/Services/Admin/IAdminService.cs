using System.Collections.Generic;
using System.Threading.Tasks;
using PROTOTYPE_backend.DTOs.Admin;

namespace PROTOTYPE_backend.Services.Admin
{
    public interface IAdminService
    {
        Task<IEnumerable<UserAdminViewDto>> GetAllUsersAsync();
        Task<bool> UpdateUserlRoleAsync(UpdateUserRoleDto dto);
        Task<bool> UpdateUserGlobalRoleAsync(UpdateUserGlobalRoleDto dto);
        Task<SystemOverviewStatsDto> GetSystemStatsAsync();
        Task<bool> ToggleUserStatusAsync(string userId);
    }
}