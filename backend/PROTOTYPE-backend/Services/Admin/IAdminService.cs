using System.Collections.Generic;
using System.Threading.Tasks;
using PROTOTYPE_backend.DTOs.Admin;

namespace PROTOTYPE_backend.Services.Admin
{
    public interface IAdminService
    {
        Task<IEnumerable<UserAdminViewDto>> GetAllUsersAsync();
        Task<bool> UpdateUserGlobalRoleAsync(UpdateUserRoleDto dto);
        Task<SystemOverviewStatsDto> GetSystemStatsAsync();
        Task<bool> ToggleUserStatusAsync(string userId); // Felhasználó inaktiválása / reaktiválása
    }
}