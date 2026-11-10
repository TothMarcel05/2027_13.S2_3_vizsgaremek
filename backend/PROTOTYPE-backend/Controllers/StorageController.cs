using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PROTOTYPE_backend.DTOs.SoftDelete;
using PROTOTYPE_backend.DTOs.Storage;
using PROTOTYPE_backend.Extensions;
using PROTOTYPE_backend.Services.SoftDelete;
using PROTOTYPE_backend.Services.Storage;

namespace PROTOTYPE_backend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/storage")]
    public class StorageController : ControllerBase
    {
        private readonly IStorageService _storageService;
        private readonly ISoftDeleteService _softDeleteService;

        public StorageController(IStorageService storageService, ISoftDeleteService softDeleteService)
        {
            _storageService = storageService;
            _softDeleteService = softDeleteService;
        }

        [HttpGet("nodes")]
        public async Task<IActionResult> GetNodes([FromQuery] Guid workspaceId, [FromQuery] string? tagFilter)
        {
            var nodes = await _storageService.GetNodesByWorkspaceAsync(workspaceId, tagFilter);
            return Ok(nodes);
        }


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

        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadFile([FromForm] UploadFileDto dto)
        {
            try
            {
                var userId = User.GetUserId();
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

        [HttpPost("delete")]
        public async Task<IActionResult> SoftDelete([FromBody] SoftDeleteRequestDto dto)
        {
            var success = await _softDeleteService.SoftDeleteAsync(dto);
            if (!success) return BadRequest("A törlés nem hajtható végre vagy az elem nem található.");

            return Ok(new { message = "Az elem sikeresen Soft-Delete állapotba került." });
        }

        [HttpPost("restore")]
        public async Task<IActionResult> Restore([FromBody] RestoreRequestDto dto)
        {
            var success = await _softDeleteService.RestoreAsync(dto);
            if (!success) return BadRequest("A visszaállítás nem lehetséges (a megőrzési idő lejárt vagy az elem nem található).");

            return Ok(new { message = "Az elem sikeresen visszaállításra került." });
        }

        [HttpGet("trash/{userId}")]
        public async Task<IActionResult> GetTrash(Guid userId)
        {
            var deletedItems = await _softDeleteService.GetDeletedItemsAsync(userId);
            return Ok(deletedItems);
        }

        [HttpDelete("purge-expired")]
        public async Task<IActionResult> PurgeExpired()
        {
            var count = await _softDeleteService.HardDeleteExpiredItemsAsync();
            return Ok(new { message = $"{count} db lejárt rekord véglegesen törölve lett." });
        }
    }
}