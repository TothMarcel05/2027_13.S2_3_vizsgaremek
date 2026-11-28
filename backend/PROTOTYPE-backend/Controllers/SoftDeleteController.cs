using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PROTOTYPE_backend.Authorization;
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

        private readonly IAuthorizationService _authorizationService;

        public SoftDeleteController(ISoftDeleteService softDeleteService, ILogger<SoftDeleteController> logger,
            IAuthorizationService authorizationService)
        {
            _authorizationService = authorizationService;
            _softDeleteService = softDeleteService;
            _logger = logger;
        }

        // 1. ELEM LOMTÁRBA HELYEZÉSE (SOFT DELETE)
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

            if (dto.EntityName == "AppUser" && !User.IsSystemAdmin())
                throw new UnauthorizedAccessException("Felhasználói fiókot csak rendszeradminisztrátor helyezhet lomtárba.");

            if (Guid.TryParse(dto.EntityId, out var entityId))
                await _authorizationService.EnsurePermissionOnAsync(User, "trash.manage", dto.EntityName, entityId);

            var success = await _softDeleteService.SoftDeleteAsync(secureDto);
            if (!success)
            {
                return BadRequest(new { error = new { message = "A törlés nem hajtható végre, érvénytelen azonosító vagy az elem nem található.", code = "ERVENYTELEN_KERES" } });
            }

            return Ok(new { message = "Az elem sikeresen lomtárba került." });
            
        }

        // 2. ELEM VISSZAÁLLÍTÁSA A LOMTÁRBÓL
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

            if (dto.EntityName == "AppUser" && !User.IsSystemAdmin())
                throw new UnauthorizedAccessException("Felhasználói fiókot csak rendszeradminisztrátor állíthat vissza.");

            if (Guid.TryParse(dto.EntityId, out var entityId))
                await _authorizationService.EnsurePermissionOnAsync(User, "trash.manage", dto.EntityName, entityId);

            var success = await _softDeleteService.RestoreAsync(dto);
            if (!success)
            {
                return BadRequest(new { error = new { message = "A visszaállítás nem lehetséges (a 30 napos megőrzési idő lejárt vagy az elem nem található).", code = "ERVENYTELEN_KERES" } });
            }

            return Ok(new { message = "Az elem sikeresen visszaállításra került." });
            
        }

        // 3. LOMTÁR TARTALMÁNAK LEKÉRDEZÉSE
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

        // 4. LEJÁRT ELEMEK VÉGLEGES TÖRLÉSE (ADMIN / KARBANTARTÁS)
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