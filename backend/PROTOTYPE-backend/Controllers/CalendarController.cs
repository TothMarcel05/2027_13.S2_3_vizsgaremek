using Microsoft.AspNetCore.Mvc;
using PROTOTYPE_backend.DTOs.Calendar;
using PROTOTYPE_backend.Services.Calendar;
using System.Security.Claims;

namespace PROTOTYPE_backend.Controllers
{
    [ApiController]
    [Route("api/calendar")]
    public class CalendarController : ControllerBase
    {
        private readonly ICalendarService _calendarService;

        public CalendarController(ICalendarService calendarService)
        {
            _calendarService = calendarService;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateDate([FromBody] DateRequestDto dto)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userIdClaim == null) return Unauthorized();

            var createdDate = await _calendarService.CreateDateAsync(dto, Guid.Parse(userIdClaim));
            return Ok(createdDate);
        }
    }
}
