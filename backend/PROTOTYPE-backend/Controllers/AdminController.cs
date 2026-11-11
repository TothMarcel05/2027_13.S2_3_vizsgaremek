using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PROTOTYPE_backend.DTOs.Admin;
using PROTOTYPE_backend.Services.Admin;

namespace PROTOTYPE_backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;

        public AdminController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        [HttpGet("roles")]
        public async Task<IActionResult> GetRolesAndPermissions()
        {
            var roles = await _adminService.GetRolesAndPermissionsAsync();
            return Ok(roles);
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _adminService.GetAllUsersAsync();
            return Ok(users);
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetSystemStats()
        {
            var stats = await _adminService.GetSystemStatsAsync();
            return Ok(stats);
        }


        [HttpPut("user/role")]

        public async Task<IActionResult> UpdateUserRole([FromBody] UpdateUserRoleDto dto)
        {
            var success = await _adminService.UpdateUserRoleAsync(dto);
            if (!success) return NotFound("A megadott felhasználó nem található.");

            return Ok(new { message = "A felhasználó szerepköre sikeresen frissítve." });
        }


        [HttpPut("user/globalRole")]
        public async Task<IActionResult> UpdateUserGlobalRole([FromBody] UpdateUserGlobalRoleDto dto)
        {
            var success = await _adminService.UpdateUserGlobalRoleAsync(dto);
            if (!success) return NotFound("A megadott felhasználó nem található.");

            return Ok(new { message = "A felhasználó szerepköre sikeresen frissítve." });
        }


        [HttpDelete("{userId}")]
        public async Task<IActionResult> RemoveUser(Guid userId)
        {
            var success = await _adminService.RemoveUserAsync(userId);
            if (!success) return NotFound("A megadott felhasználó nem található.");

            return Ok(new { message = "A felhasználó sikeresen eltávolítva." });
        }

        [HttpPut("user/{userId}/toggle-status")]
        public async Task<IActionResult> ToggleUserStatus(string userId)
        {
            var success = await _adminService.ToggleUserStatusAsync(userId);
            if (!success) return NotFound("A megadott felhasználó nem található.");

            return Ok(new { message = "A felhasználó fiókstátusza sikeresen módosítva." });
        }
    }
}