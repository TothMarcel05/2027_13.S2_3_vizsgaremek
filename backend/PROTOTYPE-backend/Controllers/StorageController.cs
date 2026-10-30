using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PROTOTYPE_backend.DTOs.Storage;
using PROTOTYPE_backend.Extensions;
using PROTOTYPE_backend.Services.Storage;

namespace PROTOTYPE_backend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/storage")]
    public class StorageController : ControllerBase
    {
        private readonly IStorageService _storageService;

        public StorageController(IStorageService storageService)
        {
            _storageService = storageService;
        }

        // POST /api/storage/upload -> Fájl feltöltése (multipart/form-data)
        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadFile([FromForm] UploadFileDto dto)
        {
            try
            {
                var userId = User.GetUserId();
                // A DTO-ba beállítjuk a bejelentkezett felhasználó ID-ját
                var updatedDto = dto with { UserId = userId };

                var result = await _storageService.UploadFileAsync(updatedDto);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // POST /api/storage/folders -> Új mappa létrehozása
        [HttpPost("folders")]
        public async Task<IActionResult> CreateFolder([FromBody] CreateFolderDto dto)
        {
            var userId = User.GetUserId();
            var folder = await _storageService.CreateFolderAsync(
                dto.WorkspaceId,
                dto.ProjectId,
                dto.ParentId,
                dto.FolderName,
                userId
            );

            return Ok(folder);
        }

        // GET /api/storage/nodes?workspaceId=guid&tagFilter=label -> Adott workspace elemeinek lekérése szűréssel
        [HttpGet("nodes")]
        public async Task<IActionResult> GetNodes([FromQuery] Guid workspaceId, [FromQuery] string? tagFilter)
        {
            var nodes = await _storageService.GetNodesByWorkspaceAsync(workspaceId, tagFilter);
            return Ok(nodes);
        }

        // GET /api/storage/nodes/{id:guid}/download -> Fájl letöltése (Stream)
        [HttpGet("nodes/{id:guid}/download")]
        public async Task<IActionResult> DownloadFile(Guid id)
        {
            var fileData = await _storageService.GetFileStreamAsync(id);

            if (fileData == null)
            {
                return NotFound(new { message = "A fájl nem található vagy törölve lett." });
            }

            var (fileStream, contentType, fileName) = fileData.Value;

            return File(fileStream, contentType, fileName);
        }

        // POST /api/storage/nodes/{id:guid}/share/{roleId:guid} -> Node megosztása szerepkörrel
        [HttpPost("nodes/{id:guid}/share/{roleId:guid}")]
        public async Task<IActionResult> ShareWithRole(Guid id, Guid roleId)
        {
            var success = await _storageService.ShareNodeWithRoleAsync(id, roleId);

            if (!success)
            {
                return NotFound(new { message = "A fájl/mappa vagy a szerepkör nem található." });
            }

            return Ok(new { message = "A hozzáférés sikeresen megadva." });
        }

        // DELETE /api/storage/nodes/{id:guid} -> Fájl vagy mappa törlése (Soft delete)
        [HttpDelete("nodes/{id:guid}")]
        public async Task<IActionResult> SoftDeleteNode(Guid id)
        {
            var userId = User.GetUserId();
            var success = await _storageService.SoftDeleteNodeAsync(id, userId);

            if (!success)
            {
                return NotFound(new { message = "A fájl vagy mappa nem található, vagy már törölve lett." });
            }

            return NoContent();
        }

        // POST /api/storage/nodes/{id:guid}/restore -> Törölt node helyreállítása
        [HttpPost("nodes/{id:guid}/restore")]
        public async Task<IActionResult> RestoreNode(Guid id)
        {
            var success = await _storageService.RestoreNodeAsync(id);

            if (!success)
            {
                return NotFound(new { message = "A fájl vagy mappa nem található, vagy nincs törölt állapotban." });
            }

            return Ok(new { message = "Sikeresen helyreállítva." });
        }
    }
}