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
            try
            {
                var userId = User.GetUserId();
                var profile = await _userService.GetUserProfileAsync(userId);

                if (profile == null)
                {
                    return NotFound(new { message = "A felhasználó nem található." });
                }

                return Ok(profile);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(new { message = "Érvénytelen vagy lejárt hitelesítés." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Váratlan hiba történt a saját profil lekérdezésekor.");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Szerveroldali hiba történt a profil lekérdezésekor." });
            }
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
                return BadRequest(new { message = "A felhasználó neve nem lehet üres." });
            }

            try
            {
                var userId = User.GetUserId();
                var updatedProfile = await _userService.UpdateUserProfileAsync(userId, dto);

                if (updatedProfile == null)
                {
                    return NotFound(new { message = "A felhasználó nem található." });
                }

                return Ok(updatedProfile);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(new { message = "Érvénytelen vagy lejárt hitelesítés." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Váratlan hiba történt a saját profil módosításakor.");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Szerveroldali hiba történt a profil frissítésekor." });
            }
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
                return BadRequest(new { message = "A jelenlegi és az új jelszó megadása kötelező." });
            }

            if (dto.NewPassword.Length < 8)
            {
                return BadRequest(new { message = "Az új jelszónak legalább 8 karakter hosszúnak kell lennie." });
            }

            if (dto.CurrentPassword == dto.NewPassword)
            {
                return BadRequest(new { message = "Az új jelszó nem egyezhet meg a jelenlegi jelszóval." });
            }

            try
            {
                var userId = User.GetUserId();
                var success = await _userService.ChangePasswordAsync(userId, dto);

                if (!success)
                {
                    return BadRequest(new { message = "A jelenlegi jelszó hibás vagy a felhasználó nem található." });
                }

                return NoContent();
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(new { message = "Érvénytelen vagy lejárt hitelesítés." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Váratlan hiba történt a jelszó módosítása során.");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Szerveroldali hiba történt a jelszó módosításakor." });
            }
        }

        // 4. DELETE /api/users/me -> Saját fiók törlése (Soft delete)
        [HttpDelete("me")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteCurrentUserAccount()
        {
            try
            {
                var userId = User.GetUserId();
                var result = await _userService.DeleteUserAccountAsync(userId);

                if (result == null)
                {
                    return NotFound(new { message = "A felhasználó nem található." });
                }

                return Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(new { message = "Érvénytelen vagy lejárt hitelesítés." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Váratlan hiba történt a felhasználói fiók törlésekor.");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Szerveroldali hiba történt a fiók törlésekor." });
            }
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
                return BadRequest(new { message = "Érvénytelen felhasználói azonosító." });
            }

            try
            {
                var publicProfile = await _userService.GetPublicProfileByIdAsync(id);

                if (publicProfile == null)
                {
                    return NotFound(new { message = "A felhasználó nem található." });
                }

                return Ok(publicProfile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Váratlan hiba történt a nyilvános profil lekérdezésekor. TargetUserId: {TargetUserId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Szerveroldali hiba történt a profil lekérdezésekor." });
            }
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
                return BadRequest(new { message = "A keresési kifejezésnek legalább 2 karakterből kell állnia." });
            }

            try
            {
                var users = await _userService.SearchUsersAsync(q);
                return Ok(users);
            }
            catch (Exception ex)
            {
                // A keresőkifejezést (q) PII-védelmi okokból nem írjuk a logba
                _logger.LogError(ex, "Váratlan hiba történt a felhasználók keresése során.");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Szerveroldali hiba történt a keresés során." });
            }
        }
    }
}