using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PROTOTYPE_backend.DTOs.SoftDelete;
using PROTOTYPE_backend.Extensions;
using PROTOTYPE_backend.Services.SoftDelete;

namespace PROTOTYPE_backend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class SoftDeleteController : ControllerBase
    {
        private readonly ISoftDeleteService _softDeleteService;
        private readonly ILogger<SoftDeleteController> _logger;

        public SoftDeleteController(ISoftDeleteService softDeleteService, ILogger<SoftDeleteController> logger)
        {
            _softDeleteService = softDeleteService;
            _logger = logger;
        }

        // 1. ELEM LOMTÁRBA HELYEZÉSE (SOFT DELETE)
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
                // A hitelesített felhasználó azonosítóját használjuk a hamisítás (spoofing) elkerülésére
                var currentUserId = User.GetUserId();
                var secureDto = dto with { UserId = currentUserId };

                var success = await _softDeleteService.SoftDeleteAsync(secureDto);
                if (!success)
                {
                    return BadRequest(new { message = "A törlés nem hajtható végre, érvénytelen azonosító vagy az elem nem található." });
                }

                return Ok(new { message = "Az elem sikeresen lomtárba került." });
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(new { message = "Érvénytelen vagy lejárt hitelesítés." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Váratlan hiba történt a SoftDelete végrehajtása közben. EntityName: {EntityName}, EntityId: {EntityId}",
                    dto.EntityName, dto.EntityId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Szerveroldali hiba történt a törlés során." });
            }
        }

        // 2. ELEM VISSZAÁLLÍTÁSA A LOMTÁRBÓL
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
                    return BadRequest(new { message = "A visszaállítás nem lehetséges (a 30 napos megőrzési idő lejárt vagy az elem nem található)." });
                }

                return Ok(new { message = "Az elem sikeresen visszaállításra került." });
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(new { message = "Érvénytelen vagy lejárt hitelesítés." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Váratlan hiba történt a visszaállítás közben. EntityName: {EntityName}, EntityId: {EntityId}",
                    dto.EntityName, dto.EntityId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Szerveroldali hiba történt a visszaállítás során." });
            }
        }

        // 3. SAJÁT LOMTÁR TARTALMÁNAK LEKÉRDEZÉSE (IDOR-védelemmel)
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

        // 4. LEJÁRT ELEMEK VÉGLEGES TÖRLÉSE (ADMIN / KARBANTARTÁS)
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
                _logger.LogError(ex, "Váratlan hiba történt a lejárt elemek végleges törlésekor (purge-expired).");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Szerveroldali hiba történt a végleges törlés során." });
            }
        }
    }
}