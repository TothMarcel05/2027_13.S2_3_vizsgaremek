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
// [Authorize(Roles = "SystemAdmin")]
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
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetRolesAndPermissions()
    {
        try
        {
            var roles = await _adminService.GetRolesAndPermissionsAsync();
            return Ok(roles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Váratlan hiba történt a szerepkörök és jogosultságok lekérdezése közben.");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Szerveroldali hiba történt a szerepkörök lekérdezésekor." });
        }
    }

    [HttpPost("roles")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateRole([FromBody] AdminCreateRoleDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            return BadRequest(new { message = "A szerepkör neve nem lehet üres." });
        }

        try
        {
            var role = await _adminService.CreateRoleAsync(dto);
            return CreatedAtAction(nameof(GetRolesAndPermissions), new { id = role.RoleId }, role);
        }
        catch (InvalidOperationException ex)
        {
            // Az AdminService ezt dobja, ha a szerepkör név már foglalt
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Váratlan hiba történt új szerepkör létrehozásakor. RoleName: {RoleName}", dto.Name);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Szerveroldali hiba történt a szerepkör létrehozásakor." });
        }
    }

    [HttpPut("roles/{id:guid}/permissions")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateRolePermissions(Guid id, [FromBody] UpdateRolePermissionsDto dto)
    {
        if (dto.Permissions == null)
        {
            return BadRequest(new { message = "A jogosultságok listája nem lehet null." });
        }

        try
        {
            var success = await _adminService.UpdateRolePermissionsAsync(id, dto);
            if (!success)
            {
                return NotFound(new { message = "A szerepkör nem található." });
            }

            return Ok(new { message = "Jogosultságok frissítve." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Váratlan hiba történt a szerepkör jogosultságainak frissítésekor. RoleId: {RoleId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Szerveroldali hiba történt a jogosultságok frissítésekor." });
        }
    }

    [HttpGet("admin/users")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAllUsers([FromQuery] int page = 1, [FromQuery] string? search = null)
    {
        if (page < 1)
        {
            return BadRequest(new { message = "Az oldalszám (page) legalább 1 kell legyen." });
        }

        try
        {
            var users = await _adminService.GetAllUsersAsync(page, search);
            return Ok(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Váratlan hiba történt a felhasználók listázásakor. Page: {Page}", page);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Szerveroldali hiba történt a felhasználók lekérdezésekor." });
        }
    }

    [HttpGet("admin/stats")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetSystemStats()
    {
        try
        {
            var stats = await _adminService.GetSystemStatsAsync();
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Váratlan hiba történt a rendszerstatisztikák lekérdezésekor.");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Szerveroldali hiba történt a statisztikák lekérdezésekor." });
        }
    }

    [HttpPut("admin/users/{id:guid}/role")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateUserGlobalRole(Guid id, [FromBody] UpdateUserGlobalRoleDto dto)
    {
        try
        {
            var success = await _adminService.UpdateUserGlobalRoleAsync(id, dto);
            if (!success)
            {
                return NotFound(new { message = "A megadott felhasználó nem található." });
            }

            return Ok(new { message = "A felhasználó szerepköre sikeresen frissítve." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Váratlan hiba történt a felhasználó globális szerepkörének módosításakor. UserId: {UserId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Szerveroldali hiba történt a szerepkör módosításakor." });
        }
    }

    // HIÁNYZÓ VÉGPONT: Felhasználói státusz váltása (felfüggesztés / újraaktiválás)
    [HttpPatch("admin/users/{id:guid}/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ToggleUserStatus(Guid id)
    {
        try
        {
            var success = await _adminService.ToggleUserStatusAsync(id);
            if (!success)
            {
                return NotFound(new { message = "A megadott felhasználó nem található." });
            }

            return Ok(new { message = "A felhasználó státusza sikeresen módosítva." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Váratlan hiba történt a felhasználó státuszának váltásakor. UserId: {UserId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Szerveroldali hiba történt a felhasználói státusz módosításakor." });
        }
    }

    [HttpDelete("admin/users/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RemoveUser(Guid id)
    {
        try
        {
            var success = await _adminService.RemoveUserAsync(id);
            if (!success)
            {
                return NotFound(new { message = "A megadott felhasználó nem található." });
            }

            return Ok(new { message = "A felhasználó sikeresen eltávolítva." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Váratlan hiba történt a felhasználó végleges törlésekor. UserId: {UserId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Szerveroldali hiba történt a felhasználó eltávolításakor." });
        }
    }
}