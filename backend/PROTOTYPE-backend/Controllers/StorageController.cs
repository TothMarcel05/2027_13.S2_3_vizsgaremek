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
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetNodes([FromQuery] Guid workspaceId, [FromQuery] string? tagFilter)
        {
            if (workspaceId == Guid.Empty)
            {
                return BadRequest(new { message = "A 'workspaceId' megadása kötelező." });
            }

            try
            {
                var nodes = await _storageService.GetNodesByWorkspaceAsync(workspaceId, tagFilter);
                return Ok(nodes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Váratlan hiba történt a tárhely elemek lekérdezésekor. WorkspaceId: {WorkspaceId}", workspaceId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Szerveroldali hiba történt a tárhely elemek lekérdezésekor." });
            }
        }

        // 2. FÁJL LETÖLTÉSE
        [HttpGet("nodes/{id:guid}/download")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DownloadFile(Guid id)
        {
            try
            {
                var fileData = await _storageService.GetFileStreamAsync(id);

                if (fileData == null)
                {
                    return NotFound(new { message = "A fájl nem található vagy törölve lett." });
                }

                var (fileStream, contentType, fileName) = fileData.Value;
                return File(fileStream, contentType, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Váratlan hiba történt a fájl letöltésekor. NodeId: {NodeId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Szerveroldali hiba történt a fájl letöltésekor." });
            }
        }

        // 3. FÁJL FELTÖLTÉSE
        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UploadFile([FromForm] UploadFileDto dto)
        {
            if (dto.File == null || dto.File.Length == 0)
            {
                return BadRequest(new { message = "A feltöltendő fájl nem lehet üres." });
            }

            if (dto.WorkspaceId == Guid.Empty)
            {
                return BadRequest(new { message = "A 'WorkspaceId' megadása kötelező." });
            }

            try
            {
                var userId = User.GetUserId();
                var updatedDto = dto with { UserId = userId };

                var result = await _storageService.UploadFileAsync(updatedDto);
                return CreatedAtAction(nameof(DownloadFile), new { id = result.Id }, result);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(new { message = "Érvénytelen vagy lejárt hitelesítés." });
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                // Kvótatúllépés esetén
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Váratlan hiba történt a fájlfeltöltés során. WorkspaceId: {WorkspaceId}", dto.WorkspaceId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Szerveroldali hiba történt a fájl feltöltésekor." });
            }
        }

        // 4. MAPPA LÉTREHOZÁSA
        [HttpPost("folders")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateFolder([FromBody] CreateFolderDto dto)
        {
            if (dto.WorkspaceId == Guid.Empty || string.IsNullOrWhiteSpace(dto.FolderName))
            {
                return BadRequest(new { message = "A munkaterület azonosítója és a mappa neve kötelező." });
            }

            try
            {
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
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(new { message = "Érvénytelen vagy lejárt hitelesítés." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Váratlan hiba történt új mappa létrehozásakor. WorkspaceId: {WorkspaceId}", dto.WorkspaceId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Szerveroldali hiba történt a mappa létrehozásakor." });
            }
        }

        // 5. CSOMÓPONT ADATAINAK MÓDOSÍTÁSA
        [HttpPut("nodes/change-data")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ChangeNodeData([FromBody] GetNodesRequestDto dto)
        {
            if (dto.NodeId == Guid.Empty)
            {
                return BadRequest(new { message = "A 'NodeId' megadása kötelező." });
            }

            try
            {
                var result = await _storageService.ChangeNodeData(dto);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Váratlan hiba történt a tárhely elem módosításakor. NodeId: {NodeId}", dto.NodeId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Szerveroldali hiba történt a módosítás során." });
            }
        }

        // 6. TÁRHELY ELEM LOMTÁRBA HELYEZÉSE
        [HttpDelete("nodes/{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SoftDeleteNode(Guid id)
        {
            try
            {
                var userId = User.GetUserId();
                var success = await _storageService.SoftDeleteNodeAsync(id, userId);

                if (!success)
                {
                    return NotFound(new { message = "A fájl vagy mappa nem található, vagy már törölve lett." });
                }

                return NoContent();
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(new { message = "Érvénytelen vagy lejárt hitelesítés." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Váratlan hiba történt a tárhely elem törlésekor. NodeId: {NodeId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Szerveroldali hiba történt a törlés során." });
            }
        }

        // 7. MEGOSZTÁS SZEREPKÖRREL
        [HttpPost("nodes/{id:guid}/share/{roleId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ShareWithRole(Guid id, Guid roleId)
        {
            try
            {
                var success = await _storageService.ShareNodeWithRoleAsync(id, roleId);

                if (!success)
                {
                    return NotFound(new { message = "A fájl/mappa vagy a szerepkör nem található." });
                }

                return Ok(new { message = "A hozzáférés sikeresen megadva." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Váratlan hiba történt a jogosultság-megosztás során. NodeId: {NodeId}, RoleId: {RoleId}", id, roleId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Szerveroldali hiba történt a megosztás során." });
            }
        }

        // 8. TÁRHELY ELEM VISSZAÁLLÍTÁSA
        [HttpPost("nodes/{id:guid}/restore")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> RestoreNode(Guid id)
        {
            try
            {
                var success = await _storageService.RestoreNodeAsync(id);

                if (!success)
                {
                    return NotFound(new { message = "A fájl vagy mappa nem található, vagy nincs törölt állapotban." });
                }

                return Ok(new { message = "Sikeresen helyreállítva." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Váratlan hiba történt a tárhely elem visszaállításakor. NodeId: {NodeId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Szerveroldali hiba történt a visszaállítás során." });
            }
        }

        // 9. ÁLTALÁNOS SOFT DELETE (LOMTÁRBA HELYEZÉS)
        [HttpPost("delete")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SoftDelete([FromBody] SoftDeleteRequestDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.EntityName) || string.IsNullOrWhiteSpace(dto.EntityId))
            {
                return BadRequest(new { message = "Az 'EntityName' és az 'EntityId' megadása kötelező." });
            }

            try
            {
                var currentUserId = User.GetUserId();
                var secureDto = dto with { UserId = currentUserId };

                var success = await _softDeleteService.SoftDeleteAsync(secureDto);
                if (!success)
                {
                    return BadRequest(new { message = "A törlés nem hajtható végre vagy az elem nem található." });
                }

                return Ok(new { message = "Az elem sikeresen Soft-Delete állapotba került." });
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(new { message = "Érvénytelen vagy lejárt hitelesítés." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Váratlan hiba történt a SoftDelete művelet során. EntityName: {EntityName}, EntityId: {EntityId}",
                    dto.EntityName, dto.EntityId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Szerveroldali hiba történt a törlés során." });
            }
        }

        // 10. ÁLTALÁNOS VISSZAÁLLÍTÁS A LOMTÁRBÓL
        [HttpPost("restore")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Restore([FromBody] RestoreRequestDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.EntityName) || string.IsNullOrWhiteSpace(dto.EntityId))
            {
                return BadRequest(new { message = "Az 'EntityName' és az 'EntityId' megadása kötelező." });
            }

            try
            {
                var success = await _softDeleteService.RestoreAsync(dto);
                if (!success)
                {
                    return BadRequest(new { message = "A visszaállítás nem lehetséges (a megőrzési idő lejárt vagy az elem nem található)." });
                }

                return Ok(new { message = "Az elem sikeresen visszaállításra került." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Váratlan hiba történt a visszaállítás során. EntityName: {EntityName}, EntityId: {EntityId}",
                    dto.EntityName, dto.EntityId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Szerveroldali hiba történt a visszaállítás során." });
            }
        }

        // 11. SAJÁT LOMTÁR LEKÉRDEZÉSE (IDOR-védelemmel)
        [HttpGet("trash")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetMyTrash()
        {
            try
            {
                var userId = User.GetUserId();
                var deletedItems = await _softDeleteService.GetDeletedItemsAsync(userId);
                return Ok(deletedItems);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(new { message = "Érvénytelen vagy lejárt hitelesítés." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Váratlan hiba történt a lomtár lekérdezésekor.");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Szerveroldali hiba történt a lomtár lekérdezésekor." });
            }
        }

        // 12. LEJÁRT REKORDOK VÉGLEGES TÖRLÉSE
        [Authorize(Roles = "SystemAdmin")]
        [HttpDelete("purge-expired")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> PurgeExpired()
        {
            try
            {
                var count = await _softDeleteService.HardDeleteExpiredItemsAsync();
                return Ok(new
                {
                    deletedCount = count,
                    message = $"{count} db lejárt rekord véglegesen törölve lett."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Váratlan hiba történt a lejárt rekordok végleges törlésekor.");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Szerveroldali hiba történt a végleges törlés során." });
            }
        }
    }
}