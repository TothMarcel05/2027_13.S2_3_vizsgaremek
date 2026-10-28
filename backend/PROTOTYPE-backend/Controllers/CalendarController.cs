using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PROTOTYPE_backend.DTOs.Calendar;
using PROTOTYPE_backend.Extensions;
using PROTOTYPE_backend.Services.Calendar;

namespace PROTOTYPE_backend.Controllers
{
    [Authorize] // Minden végponthoz bejelentkezés szükséges
    [ApiController]
    [Route("api/calendar")]
    public class CalendarController : ControllerBase
    {
        private readonly ICalendarService _calendarService;

        public CalendarController(ICalendarService calendarService)
        {
            _calendarService = calendarService;
        }

        // 1. LÉTREHOZÁS
        [HttpPost]
        public async Task<IActionResult> CreateDate([FromBody] DateRequestDto dto)
        {
            try
            {
                var userId = User.GetUserId();
                var createdDate = await _calendarService.CreateDateAsync(dto, userId);

                // RESTful válasz: 201 Created + a lekérdező URL
                return CreatedAtAction(nameof(GetDateById), new { id = createdDate.Id }, createdDate);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // 2. MÓDOSÍTÁS
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateDate(Guid id, [FromBody] DateUpdateDto dto)
        {
            try
            {
                var updatedDate = await _calendarService.UpdateDateAsync(id, dto);

                if (updatedDate == null)
                {
                    return NotFound(new { message = "A megadott naptáresemény nem található." });
                }

                return Ok(updatedDate);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // 3. TÖRLEÉS
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteDate(Guid id)
        {
            var success = await _calendarService.DeleteDateAsync(id);

            if (!success)
            {
                return NotFound(new { message = "A törölni kívánt naptáresemény nem található." });
            }

            return NoContent(); // 204 No Content sikeres törlés esetén
        }

        // 4. EGYEDI LEKÉRDEZÉS ID ALAPJÁN
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetDateById(Guid id)
        {
            var dateDto = await _calendarService.GetDateByIdAsync(id);

            if (dateDto == null)
            {
                return NotFound(new { message = "A naptáresemény nem található." });
            }

            return Ok(dateDto);
        }

        // 5. INTERVALLUM ALAPÚ LISTÁZÁS (Frontend naptár nézethez)
        // Példa hívás: GET /api/calendar?start=2026-10-01T00:00:00Z&end=2026-10-31T23:59:59Z&projectId=guid
        [HttpGet]
        public async Task<IActionResult> GetDatesByRange(
            [FromQuery] DateTime start,
            [FromQuery] DateTime end,
            [FromQuery] Guid? projectId = null)
        {
            try
            {
                var userId = User.GetUserId();
                var dates = await _calendarService.GetDatesByRangeAsync(start, end, userId, projectId);
                return Ok(dates);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}