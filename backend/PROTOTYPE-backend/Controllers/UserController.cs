using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PROTOTYPE_backend.DTOs.Users;
using PROTOTYPE_backend.Extensions;
using PROTOTYPE_backend.Services.Users;

namespace PROTOTYPE_backend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/users")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<UserController> _logger;

        public UserController(IUserService userService, ILogger<UserController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        // 1. GET /api/users/me -> Saját adatok lekérése
        [HttpGet("me")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetCurrentUserProfile()
        {
            var userId = User.GetUserId();
            var profile = await _userService.GetUserProfileAsync(userId);

            if (profile == null)
            {
                return NotFound(new { error = new { message = "A felhasználó nem található.", code = "NEM_TALALHATO" } });
            }

            return Ok(profile);
            
        }

        // 2. PUT /api/users/me -> Saját adatok frissítése
        [HttpPut("me")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateCurrentUserProfile([FromBody] UserModifyRequestDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                return BadRequest(new { error = new { message = "A felhasználó neve nem lehet üres.", code = "ERVENYTELEN_KERES" } });
            }

            var userId = User.GetUserId();
            var updatedProfile = await _userService.UpdateUserProfileAsync(userId, dto);

            if (updatedProfile == null)
            {
                return NotFound(new { error = new { message = "A felhasználó nem található.", code = "NEM_TALALHATO" } });
            }

            return Ok(updatedProfile);
            
        }

        // 3. POST /api/users/me/change-password -> Jelszó módosítása
        [HttpPost("me/change-password")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.CurrentPassword) || string.IsNullOrWhiteSpace(dto.NewPassword))
            {
                return BadRequest(new { error = new { message = "A jelenlegi és az új jelszó megadása kötelező.", code = "ERVENYTELEN_KERES" } });
            }

            if (dto.NewPassword.Length < 8)
            {
                return BadRequest(new { error = new { message = "Az új jelszónak legalább 8 karakter hosszúnak kell lennie.", code = "ERVENYTELEN_KERES" } });
            }

            if (dto.CurrentPassword == dto.NewPassword)
            {
                return BadRequest(new { error = new { message = "Az új jelszó nem egyezhet meg a jelenlegi jelszóval.", code = "ERVENYTELEN_KERES" } });
            }

            var userId = User.GetUserId();
            var success = await _userService.ChangePasswordAsync(userId, dto);

            if (!success)
            {
                return BadRequest(new { error = new { message = "A jelenlegi jelszó hibás vagy a felhasználó nem található.", code = "ERVENYTELEN_KERES" } });
            }

            return NoContent();
            
        }

        // 4. DELETE /api/users/me -> Saját fiók törlése (Soft delete)
        [HttpDelete("me")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteCurrentUserAccount()
        {
            var userId = User.GetUserId();
            var result = await _userService.DeleteUserAccountAsync(userId);

            if (result == null)
            {
                return NotFound(new { error = new { message = "A felhasználó nem található.", code = "NEM_TALALHATO" } });
            }

            return Ok(result);
            
        }

        // 5. GET /api/users/{id} -> Egy adott csapattag publikus profilja
        [HttpGet("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetPublicProfile(Guid id)
        {
            if (id == Guid.Empty)
            {
                return BadRequest(new { error = new { message = "Érvénytelen felhasználói azonosító.", code = "ERVENYTELEN_KERES" } });
            }

            var publicProfile = await _userService.GetPublicProfileByIdAsync(id);

            if (publicProfile == null)
            {
                return NotFound(new { error = new { message = "A felhasználó nem található.", code = "NEM_TALALHATO" } });
            }

            return Ok(publicProfile);
            
        }

        // 6. GET /api/users/search?q=kovacs -> Csapattagok keresése
        [HttpGet("search")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SearchUsers([FromQuery] string q)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 2)
            {
                return BadRequest(new { error = new { message = "A keresési kifejezésnek legalább 2 karakterből kell állnia.", code = "ERVENYTELEN_KERES" } });
            }

            var users = await _userService.SearchUsersAsync(q);
            return Ok(users);
            
        }
    }
}