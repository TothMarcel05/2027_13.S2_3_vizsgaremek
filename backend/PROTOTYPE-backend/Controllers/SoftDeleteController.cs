using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PROTOTYPE_backend.DTOs.SoftDelete;
using PROTOTYPE_backend.Services.SoftDelete;

namespace PROTOTYPE_backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SoftDeleteController : ControllerBase
    {
        private readonly ISoftDeleteService _softDeleteService;

        public SoftDeleteController(ISoftDeleteService softDeleteService)
        {
            _softDeleteService = softDeleteService;
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
        public async Task<IActionResult> GetTrash(string userId)
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