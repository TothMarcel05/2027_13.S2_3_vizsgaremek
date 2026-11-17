using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PROTOTYPE_backend.DTOs.Admin;
using PROTOTYPE_backend.Services.Admin;

namespace PROTOTYPE_backend.Controllers;

[ApiController]
[Route("api")]
// [Authorize(Roles = "SystemAdmin")] 
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

    [HttpPost("roles")]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleDto dto)
    {
        var role = await _adminService.CreateRoleAsync(dto);
        return CreatedAtAction(nameof(GetRolesAndPermissions), new { id = role.RoleId }, role);
    }

    [HttpPut("roles/{id}/permissions")]
    public async Task<IActionResult> UpdateRolePermissions(Guid id, [FromBody] UpdateRolePermissionsDto dto)
    {
        var success = await _adminService.UpdateRolePermissionsAsync(id, dto);
        if (!success) return NotFound(new { message = "A szerepkör nem található." });

        return Ok(new { message = "Jogosultságok frissítve." });
    }

    [HttpGet("admin/users")]
    public async Task<IActionResult> GetAllUsers([FromQuery] int page = 1, [FromQuery] string? search = null)
    {
        var users = await _adminService.GetAllUsersAsync(page, search);
        return Ok(users);
    }

    [HttpGet("admin/stats")]
    public async Task<IActionResult> GetSystemStats()
    {
        var stats = await _adminService.GetSystemStatsAsync();
        return Ok(stats);
    }

    [HttpPut("admin/users/{id}/role")]
    public async Task<IActionResult> UpdateUserGlobalRole(Guid id, [FromBody] UpdateUserGlobalRoleDto dto)
    {
        var success = await _adminService.UpdateUserGlobalRoleAsync(id, dto);
        if (!success) return NotFound(new { message = "A megadott felhasználó nem található." });

        return Ok(new { message = "A felhasználó szerepköre sikeresen frissítve." });
    }

    [HttpDelete("admin/users/{id}")]
    public async Task<IActionResult> RemoveUser(Guid id)
    {

        var success = await _adminService.RemoveUserAsync(id);
        if (!success) return NotFound(new { message = "A megadott felhasználó nem található." });

        return Ok(new { message = "A felhasználó sikeresen eltávolítva." });
    }
}