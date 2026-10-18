using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PROTOTYPE_backend.DTOs.Storage;
using PROTOTYPE_backend.Services.Storage;

namespace PROTOTYPE_backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StorageController : ControllerBase
    {
        private readonly IStorageService _storageService;

        public StorageController(IStorageService storageService)
        {
            _storageService = storageService;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile([FromForm] UploadFileDto dto)
        {
            if (dto.File == null || dto.File.Length == 0)
                return BadRequest("Érvénytelen vagy üres fájl.");

            var result = await _storageService.UploadFileAsync(dto);
            return Ok(result);
        }

        [HttpGet("workspace/{workspaceId}")]
        public async Task<IActionResult> GetFiles(Guid workspaceId, [FromQuery] string? tag)
        {
            var files = await _storageService.GetNodesByWorkspaceAsync(workspaceId, tag);
            return Ok(files);
        }

        [HttpPost("share")]
        public async Task<IActionResult> ShareNode([FromBody] ShareNodeDto dto)
        {
            var success = await _storageService.ShareNodeWithRoleAsync(dto.NodeId, dto.RoleId);
            if (!success) return NotFound("A fájl vagy szerepkör nem található.");
            return Ok(new { message = "Sikeres megosztás." });
        }

        [HttpDelete("{nodeId}")]
        public async Task<IActionResult> SoftDeleteNode(Guid nodeId, [FromQuery] string userId)
        {
            var success = await _storageService.SoftDeleteNodeAsync(nodeId, userId);
            if (!success) return NotFound("A fájl nem található vagy már törölve van.");
            return Ok(new { message = "Fájl sikeresen Soft-Delete állapotba került." });
        }

        [HttpPost("{nodeId}/restore")]
        public async Task<IActionResult> RestoreNode(Guid nodeId)
        {
            var success = await _storageService.RestoreNodeAsync(nodeId);
            if (!success) return NotFound("A fájl nem található vagy nincs törölve.");
            return Ok(new { message = "Fájl sikeresen visszaállítva." });
        }
    }
}