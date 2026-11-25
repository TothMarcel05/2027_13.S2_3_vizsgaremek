using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<StorageController> _logger;

        public StorageController(
            IStorageService storageService,
            ISoftDeleteService softDeleteService,
            ILogger<StorageController> logger)
        {
            _storageService = storageService;
            _softDeleteService = softDeleteService;
            _logger = logger;
        }

        // 1. MUNKATERÜLET FÁJLJAINAK ÉS MAPPÁINAK LISTÁZÁSA
        [HttpGet("nodes")]
        [Authorize(Policy = "storage.read")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetNodes([FromQuery] Guid workspaceId, [FromQuery] string? tagFilter)
        {
            if (workspaceId == Guid.Empty)
            {
                return BadRequest(new { error = new { message = "A 'workspaceId' megadása kötelező.", code = "ERVENYTELEN_KERES" } });
            }

            var nodes = await _storageService.GetNodesByWorkspaceAsync(workspaceId, tagFilter);
            return Ok(nodes);
            
        }

        // 2. FÁJL LETÖLTÉSE
        [HttpGet("nodes/{id:guid}/download")]
        [Authorize(Policy = "storage.read")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DownloadFile(Guid id)
        {
            var fileData = await _storageService.GetFileStreamAsync(id);

            if (fileData == null)
            {
                return NotFound(new { error = new { message = "A fájl nem található vagy törölve lett.", code = "NEM_TALALHATO" } });
            }

            var (fileStream, contentType, fileName) = fileData.Value;
            return File(fileStream, contentType, fileName);
            
        }

        // 3. FÁJL FELTÖLTÉSE
        [HttpPost("upload")]
        [Authorize(Policy = "storage.write")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UploadFile([FromForm] UploadFileDto dto)
        {
            if (dto.File == null || dto.File.Length == 0)
            {
                return BadRequest(new { error = new { message = "A feltöltendő fájl nem lehet üres.", code = "ERVENYTELEN_KERES" } });
            }

            if (dto.WorkspaceId == Guid.Empty)
            {
                return BadRequest(new { error = new { message = "A 'WorkspaceId' megadása kötelező.", code = "ERVENYTELEN_KERES" } });
            }

            var userId = User.GetUserId();
            var updatedDto = dto with { UserId = userId };

            var result = await _storageService.UploadFileAsync(updatedDto);
            return CreatedAtAction(nameof(DownloadFile), new { id = result.Id }, result);
            
        }

        // 4. MAPPA LÉTREHOZÁSA
        [HttpPost("folders")]
        [Authorize(Policy = "storage.write")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateFolder([FromBody] CreateFolderDto dto)
        {
            if (dto.WorkspaceId == Guid.Empty || string.IsNullOrWhiteSpace(dto.FolderName))
            {
                return BadRequest(new { error = new { message = "A munkaterület azonosítója és a mappa neve kötelező.", code = "ERVENYTELEN_KERES" } });
            }

            var userId = User.GetUserId();
            var folder = await _storageService.CreateFolderAsync(
                dto.WorkspaceId,
                dto.ProjectId,
                dto.ParentId,
                dto.FolderName,
                userId
            );

            return StatusCode(StatusCodes.Status201Created, folder);
            
        }

        // 5. CSOMÓPONT ADATAINAK MÓDOSÍTÁSA
        [HttpPut("nodes/change-data")]
        [Authorize(Policy = "storage.write")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ChangeNodeData([FromBody] GetNodesRequestDto dto)
        {
            if (dto.NodeId == Guid.Empty)
            {
                return BadRequest(new { error = new { message = "A 'NodeId' megadása kötelező.", code = "ERVENYTELEN_KERES" } });
            }

            var result = await _storageService.ChangeNodeData(dto);
            return Ok(result);
            
        }

        // 6. TÁRHELY ELEM LOMTÁRBA HELYEZÉSE
        [HttpDelete("nodes/{id:guid}")]
        [Authorize(Policy = "storage.delete")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SoftDeleteNode(Guid id)
        {
            var userId = User.GetUserId();
            var success = await _storageService.SoftDeleteNodeAsync(id, userId);

            if (!success)
            {
                return NotFound(new { error = new { message = "A fájl vagy mappa nem található, vagy már törölve lett.", code = "NEM_TALALHATO" } });
            }

            return NoContent();
            
        }

        // 7. MEGOSZTÁS SZEREPKÖRREL
        [HttpPost("nodes/{id:guid}/share/{roleId:guid}")]
        [Authorize(Policy = "storage.share")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ShareWithRole(Guid id, Guid roleId)
        {
            var success = await _storageService.ShareNodeWithRoleAsync(id, roleId);

            if (!success)
            {
                return NotFound(new { error = new { message = "A fájl/mappa vagy a szerepkör nem található.", code = "NEM_TALALHATO" } });
            }

            return Ok(new { message = "A hozzáférés sikeresen megadva." });
            
        }

        // 8. TÁRHELY ELEM VISSZAÁLLÍTÁSA
        [HttpPost("nodes/{id:guid}/restore")]
        [Authorize(Policy = "trash.manage")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> RestoreNode(Guid id)
        {
            var success = await _storageService.RestoreNodeAsync(id);

            if (!success)
            {
                return NotFound(new { error = new { message = "A fájl vagy mappa nem található, vagy nincs törölt állapotban.", code = "NEM_TALALHATO" } });
            }

            return Ok(new { message = "Sikeresen helyreállítva." });
            
        }

        // 9. ÁLTALÁNOS SOFT DELETE (LOMTÁRBA HELYEZÉS)
        [HttpPost("delete")]
        [Authorize(Policy = "trash.manage")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SoftDelete([FromBody] SoftDeleteRequestDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.EntityName) || string.IsNullOrWhiteSpace(dto.EntityId))
            {
                return BadRequest(new { error = new { message = "Az 'EntityName' és az 'EntityId' megadása kötelező.", code = "ERVENYTELEN_KERES" } });
            }

            var currentUserId = User.GetUserId();
            var secureDto = dto with { UserId = currentUserId };

            var success = await _softDeleteService.SoftDeleteAsync(secureDto);
            if (!success)
            {
                return BadRequest(new { error = new { message = "A törlés nem hajtható végre vagy az elem nem található.", code = "ERVENYTELEN_KERES" } });
            }

            return Ok(new { message = "Az elem sikeresen Soft-Delete állapotba került." });
            
        }

        // 10. ÁLTALÁNOS VISSZAÁLLÍTÁS A LOMTÁRBÓL
        [HttpPost("restore")]
        [Authorize(Policy = "trash.manage")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Restore([FromBody] RestoreRequestDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.EntityName) || string.IsNullOrWhiteSpace(dto.EntityId))
            {
                return BadRequest(new { error = new { message = "Az 'EntityName' és az 'EntityId' megadása kötelező.", code = "ERVENYTELEN_KERES" } });
            }

            var success = await _softDeleteService.RestoreAsync(dto);
            if (!success)
            {
                return BadRequest(new { error = new { message = "A visszaállítás nem lehetséges (a megőrzési idő lejárt vagy az elem nem található).", code = "ERVENYTELEN_KERES" } });
            }

            return Ok(new { message = "Az elem sikeresen visszaállításra került." });
            
        }

        // 11. LOMTÁR LEKÉRDEZÉSE
        [HttpGet("trash")]
        [Authorize(Policy = "trash.manage")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetMyTrash()
        {
            var userId = User.GetUserId();
            var deletedItems = await _softDeleteService.GetDeletedItemsAsync(userId);
            return Ok(deletedItems);
            
        }

        // 12. LEJÁRT REKORDOK VÉGLEGES TÖRLÉSE
        [HttpDelete("purge-expired")]
        [Authorize(Policy = "system.trash.purge")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> PurgeExpired()
        {
            var count = await _softDeleteService.HardDeleteExpiredItemsAsync();
            return Ok(new
            {
                deletedCount = count,
                message = $"{count} db lejárt rekord véglegesen törölve lett."
            });
            
        }
    }
}