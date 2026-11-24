using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PROTOTYPE_backend.DTOs.Calendar;
using PROTOTYPE_backend.Extensions;
using PROTOTYPE_backend.Services.Calendar;

namespace PROTOTYPE_backend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/calendar")]
    public class CalendarController : ControllerBase
    {
        private readonly ICalendarService _calendarService;
        private readonly ILogger<CalendarController> _logger;

        public CalendarController(ICalendarService calendarService, ILogger<CalendarController> logger)
        {
            _calendarService = calendarService;
            _logger = logger;
        }

        // 1. LÉTREHOZÁS
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateDate([FromBody] DateRequestDto dto)
        {
            try
            {
                var userId = User.GetUserId();
                var createdDate = await _calendarService.CreateDateAsync(dto, userId);

                return CreatedAtAction(nameof(GetDateById), new { id = createdDate.Id }, createdDate);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Váratlan hiba történt a naptáresemény létrehozásakor.");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Szerveroldali hiba történt a naptáresemény létrehozásakor." });
            }
        }

        // 2. MÓDOSÍTÁS
        [HttpPut("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateDate(Guid id, [FromBody] DateUpdateDto dto)
        {
            try
            {
                var userId = User.GetUserId();
                var updatedDate = await _calendarService.UpdateDateAsync(id, dto, userId);

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
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Váratlan hiba történt a naptáresemény módosításakor. EventId: {EventId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Szerveroldali hiba történt a naptáresemény módosításakor." });
            }
        }

        // 3. TÖRLÉS
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteDate(Guid id)
        {
            try
            {
                var userId = User.GetUserId();
                var success = await _calendarService.DeleteDateAsync(id, userId);

                if (!success)
                {
                    return NotFound(new { message = "A törölni kívánt naptáresemény nem található." });
                }

                return NoContent();
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Váratlan hiba történt a naptáresemény törlésekor. EventId: {EventId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Szerveroldali hiba történt a naptáresemény törlésekor." });
            }
        }

        // 4. EGYEDI LEKÉRDEZÉS ID ALAPJÁN
        [HttpGet("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetDateById(Guid id)
        {
            try
            {
                var userId = User.GetUserId();
                var dateDto = await _calendarService.GetDateByIdAsync(id, userId);

                if (dateDto == null)
                {
                    return NotFound(new { message = "A naptáresemény nem található." });
                }

                return Ok(dateDto);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Váratlan hiba történt a naptáresemény lekérdezésekor. EventId: {EventId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Szerveroldali hiba történt a naptáresemény lekérdezésekor." });
            }
        }

        // 5. INTERVALLUM ALAPÚ LISTÁZÁS (Frontend naptár nézethez)
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetDatesByRange(
            [FromQuery] DateTime start,
            [FromQuery] DateTime end,
            [FromQuery] Guid? projectId = null)
        {
            if (start == default || end == default)
            {
                return BadRequest(new { message = "A 'start' és 'end' dátumparaméterek megadása kötelező." });
            }

            if (end < start)
            {
                return BadRequest(new { message = "A záró dátum ('end') nem lehet korábbi a kezdő dátumnál ('start')." });
            }

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
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Váratlan hiba történt a naptáresemények intervallum-lekérdezésekor.");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Szerveroldali hiba történt a naptáresemények lekérdezésekor." });
            }
        }
    }
}