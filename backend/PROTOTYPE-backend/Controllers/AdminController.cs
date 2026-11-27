using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PROTOTYPE_backend.DTOs.Admin;
using PROTOTYPE_backend.Services.Admin;

namespace PROTOTYPE_backend.Controllers;

[ApiController]
[Route("api")]
[Authorize] // Alapértelmezetten minden végpont hitelesített tokent igényel
public class AdminController : ControllerBase
{
    private readonly IAdminService _adminService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(IAdminService adminService, ILogger<AdminController> logger)
    {
        _adminService = adminService;
        _logger = logger;
    }

    [HttpGet("roles")]
    [Authorize(Policy = "workspace.roles.manage")] // SystemAdmin ÉS Workspace Owner/Admin is elérheti
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetRolesAndPermissions()
    {
        var roles = await _adminService.GetRolesAndPermissionsAsync();
        return Ok(roles);
        
    }

    [HttpPost("roles")]
    [Authorize(Policy = "system.roles.manage")] // Ha Workspace Adminnak is engeded, írd át "workspace.roles.manage"-re
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateRole([FromBody] AdminCreateRoleDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            return BadRequest(new { error = new { message = "A szerepkör neve nem lehet üres.", code = "ERVENYTELEN_KERES" } });
        }

        var role = await _adminService.CreateRoleAsync(dto);
        return CreatedAtAction(nameof(GetRolesAndPermissions), new { id = role.RoleId }, role);
        
    }

    [HttpPut("roles/{id:guid}/permissions")]
    [Authorize(Policy = "system.roles.manage")] // Ha Workspace Adminnak is engeded, írd át "workspace.roles.manage"-re
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateRolePermissions(Guid id, [FromBody] UpdateRolePermissionsDto dto)
    {
        if (dto.Permissions == null)
        {
            return BadRequest(new { error = new { message = "A jogosultságok listája nem lehet null.", code = "ERVENYTELEN_KERES" } });
        }

        var success = await _adminService.UpdateRolePermissionsAsync(id, dto);
        if (!success)
        {
            return NotFound(new { error = new { message = "A szerepkör nem található.", code = "NEM_TALALHATO" } });
        }

        return Ok(new { message = "Jogosultságok frissítve." });
        
    }

    [HttpGet("admin/users")]
    [Authorize(Policy = "system.users.manage")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAllUsers([FromQuery] int page = 1, [FromQuery] string? search = null)
    {
        if (page < 1)
        {
            return BadRequest(new { error = new { message = "Az oldalszám (page) legalább 1 kell legyen.", code = "ERVENYTELEN_KERES" } });
        }

        var users = await _adminService.GetAllUsersAsync(page, search);
        return Ok(users);
        
    }

    [HttpGet("admin/stats")]
    [Authorize(Policy = "system.stats.read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetSystemStats()
    {
        var stats = await _adminService.GetSystemStatsAsync();
        return Ok(stats);
        
    }

    [HttpPut("admin/users/{id:guid}/role")]
    [Authorize(Policy = "system.users.manage")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateUserGlobalRole(Guid id, [FromBody] UpdateUserGlobalRoleDto dto)
    {
        var success = await _adminService.UpdateUserGlobalRoleAsync(id, dto);
        if (!success)
        {
            return NotFound(new { error = new { message = "A megadott felhasználó nem található.", code = "NEM_TALALHATO" } });
        }

        return Ok(new { message = "A felhasználó szerepköre sikeresen frissítve." });
        
    }

    [HttpPatch("admin/users/{id:guid}/status")]
    [Authorize(Policy = "system.users.manage")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ToggleUserStatus(Guid id)
    {
        var success = await _adminService.ToggleUserStatusAsync(id);
        if (!success)
        {
            return NotFound(new { error = new { message = "A megadott felhasználó nem található.", code = "NEM_TALALHATO" } });
        }

        return Ok(new { message = "A felhasználó státusza sikeresen módosítva." });
        
    }

    [HttpDelete("admin/users/{id:guid}")]
    [Authorize(Policy = "system.users.manage")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RemoveUser(Guid id)
    {
        var success = await _adminService.RemoveUserAsync(id);
        if (!success)
        {
            return NotFound(new { error = new { message = "A megadott felhasználó nem található.", code = "NEM_TALALHATO" } });
        }

        return Ok(new { message = "A felhasználó sikeresen eltávolítva." });
        
    }
}