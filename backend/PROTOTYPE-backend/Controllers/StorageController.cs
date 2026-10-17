using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PROTOTYPE_backend.DTOs.Storage;
using PROTOTYPE_backend.Services.Storage;
using System.Security.Claims;

namespace PROTOTYPE_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
//[Authorize]
public class StorageController : ControllerBase
{
    private readonly IStorageService _storageService;

    public StorageController(IStorageService storageService)
    {
        _storageService = storageService;
    }

    private Guid GetUserId() => Guid.Parse("00000000-0000-0000-0000-000000000001");
    //Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost("upload")]
    public async Task<IActionResult> Upload([FromBody] FileUploadDto dto)
    {
        var result = await _storageService.UploadFileAsync(dto, GetUserId());
        return Ok(result);
    }

    [HttpGet("search")]
    public async Task<IActionResult> SearchByTag([FromQuery] string tag, [FromQuery] Guid? workspaceId)
    {
        var result = await _storageService.GetFilesByTagAsync(tag, workspaceId);
        return Ok(result);
    }

    [HttpPost("rbac")]
    public async Task<IActionResult> SetTagRbac([FromBody] SetTagRbacDto dto)
    {
        await _storageService.SetTagRbacAsync(dto);
        return Ok(new { message = "Címke RBAC jogosultságok sikeresen beállítva." });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteFile(Guid id)
    {
        var success = await _storageService.DeleteFileAsync(id, GetUserId());
        if (!success) return NotFound("A fájl nem található vagy már törölve lett.");
        return Ok(new { message = "Fájl áthelyezve a lomtárba (Soft-Delete)." });
    }
}