using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PROTOTYPE_backend.DTOs.Admin;
using PROTOTYPE_backend.Services.Admin;

namespace PROTOTYPE_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] 
public class AdminController : ControllerBase
{
    private readonly IAdminService _adminService;

    public AdminController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _adminService.GetAllUsersAsync();
        return Ok(users);
    }

    [HttpPatch("user/role")]
    public async Task<IActionResult> UpdateUserRole([FromBody] UpdateGlobalRoleDto dto)
    {
        var success = await _adminService.UpdateGlobalRoleAsync(dto);
        if (!success) return BadRequest("A felhasználó nem található vagy érvénytelen a szerepkör.");
        return Ok(new { message = "Felhasználói szerepkör sikeresen frissítve." });
    }

    [HttpGet("trash")]
    public async Task<IActionResult> GetTrashItems()
    {
        var items = await _adminService.GetSoftDeletedItemsAsync();
        return Ok(items);
    }

    [HttpPost("restore")]
    public async Task<IActionResult> RestoreItem([FromBody] RestoreItemDto dto)
    {
        var success = await _adminService.RestoreDeletedItemAsync(dto);
        if (!success) return NotFound("Az elem nem található vagy nem állítható vissza.");
        return Ok(new { message = "Elem sikeresen visszaállítva." });
    }

    [HttpPost("purge")]
    public async Task<IActionResult> PurgeExpiredItems([FromQuery] int retentionDays = 30)
    {
        await _adminService.PurgeExpiredDeletedItemsAsync(retentionDays);
        return Ok(new { message = $"A {retentionDays} napnál régebben törölt elemek véglegesen törölve lettek az adatbázisból." });
    }
}