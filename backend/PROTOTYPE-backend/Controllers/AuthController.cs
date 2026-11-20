using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PROTOTYPE_backend.DTOs.Auth;
using PROTOTYPE_backend.Extensions;
using PROTOTYPE_backend.Services.Auth;

namespace PROTOTYPE_backend.Controllers
{
    [Route("api/auth")]
    [ApiController]
    [Authorize]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        // 1. REGISZTRÁCIÓ
        [AllowAnonymous]
        [HttpPost("register")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            try
            {
                var result = await _authService.CreateUserAsync(dto);
                return StatusCode(StatusCodes.Status201Created, result);
            }
            catch (Exception ex) when (ex.Message.Contains("foglalt"))
            {
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Váratlan hiba történt a regisztráció során.");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Szerveroldali hiba történt a regisztráció során." });
            }
        }

        // 2. BEJELENTKEZÉS
        [AllowAnonymous]
        [HttpPost("login")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            try
            {
                var result = await _authService.LoginAsync(dto);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Váratlan hiba történt a bejelentkezés során.");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Szerveroldali hiba történt a bejelentkezés során." });
            }
        }

        // 3. 2FA BEÁLLÍTÁS KEZDÉSE (TOTP)
        [HttpPost("2fa/setup")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> TwoFactorSetup()
        {
            try
            {
                var userId = User.GetUserId();
                var result = await _authService.BeginTwoFactorSetupAsync(userId);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Váratlan hiba történt a 2FA beállítás indításakor.");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Szerveroldali hiba történt." });
            }
        }

        // 4. 2FA ENGEDÉLYEZÉSE / VERIFIKÁCIÓJA SETUP UTÁN (TOTP)
        [HttpPost("2fa/enable")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> EnableTwoFactor([FromBody] TwoFactorCodeDto dto)
        {
            try
            {
                var userId = User.GetUserId();
                var recoveryCodes = await _authService.EnableTwoFactorAsync(userId, dto.Code);
                return Ok(new { recoveryCodes });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Váratlan hiba történt a TOTP 2FA aktiválásakor.");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Szerveroldali hiba történt." });
            }
        }

        // 5. 2FA BEJELENTKEZÉS IGAZOLÁSA
        [AllowAnonymous]
        [HttpPost("2fa/verify")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> VerifyTwoFactor([FromBody] TwoFactorLoginDto dto)
        {
            try
            {
                var result = await _authService.VerifyTwoFactorAsync(dto);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Váratlan hiba történt a 2FA kód ellenőrzésekor.");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Szerveroldali hiba történt." });
            }
        }

        // HIÁNYZÓ VÉGPONT: E-mailes 2FA kód újraküldése (30 mp rate limit-tel)
        [AllowAnonymous]
        [HttpPost("2fa/resend-email")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ResendTwoFactorEmail([FromBody] string twoFactorToken)
        {
            try
            {
                await _authService.ResendTwoFactorEmailCodeAsync(twoFactorToken);
                return Ok(new { message = "Az új biztonsági kód elküldve e-mailben." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("30 másodperc"))
            {
                return StatusCode(StatusCodes.Status429TooManyRequests, new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Váratlan hiba történt az e-mailes 2FA kód újraküldésekor.");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Szerveroldali hiba történt." });
            }
        }

        // 6. 2FA KIKAPCSOLÁSA (await javítva!)
        [HttpDelete("2fa/disable")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DisableTwoFactor([FromBody] string password)
        {
            try
            {
                var userId = User.GetUserId();
                var result = await _authService.DisableTwoFactorAsync(userId, password);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Váratlan hiba történt a 2FA kikapcsolásakor.");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Szerveroldali hiba történt." });
            }
        }

        // 7. E-MAILES 2FA ENGEDÉLYEZÉSE
        [HttpPost("me/2fa/enable-email")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> EnableEmailTwoFactor([FromBody] EnableEmailTwoFactorDto dto)
        {
            try
            {
                var userId = User.GetUserId();
                var recoveryCodes = await _authService.EnableEmailTwoFactorAsync(userId, dto.Password);
                return Ok(new { recoveryCodes });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Váratlan hiba történt az e-mailes 2FA aktiválásakor.");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Szerveroldali hiba történt." });
            }
        }

        // 8. HELYREÁLLÍTÓ KÓDOK ÚJRAGENERÁLÁSA
        [HttpPost("me/2fa/recovery-codes")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> RegenerateRecoveryCodes([FromBody] RegenerateRecoveryCodesDto dto)
        {
            try
            {
                var userId = User.GetUserId();
                var recoveryCodes = await _authService.RegenerateRecoveryCodesAsync(userId, dto.Password);
                return Ok(new { recoveryCodes });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Váratlan hiba történt a helyreállító kódok újragenerálásakor.");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Szerveroldali hiba történt." });
            }
        }

        // 9. TOKEN FRISSÍTÉS
        [AllowAnonymous]
        [HttpPost("refresh")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequestDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.RefreshToken))
            {
                return BadRequest(new { message = "A refresh token megadása kötelező." });
            }

            try
            {
                var result = await _authService.RefreshAsync(dto.RefreshToken);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        // 10. KIJELENTKEZÉS
        [HttpPost("logout")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Logout([FromBody] RefreshTokenRequestDto dto)
        {
            try
            {
                var userId = User.GetUserId();
                await _authService.LogoutAsync(userId, dto.RefreshToken);
                return NoContent();
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Váratlan hiba történt a kijelentkezés során.");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Szerveroldali hiba történt." });
            }
        }

        // 11. JOGOSULTSÁGOK KATEGÓRIA SZERINT (visszatérési érték javítva!)
        [HttpGet("permissions")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetPermissions([FromQuery] string query)
        {
            try
            {
                var result = await _authService.GetPermissionsByCatAsync(query);
                return Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // 12. SZEREPKÖRÖK LISTÁZÁSA (visszatérési érték javítva!)
        [HttpGet("roles")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetRoles([FromQuery] string query = "")
        {
            try
            {
                var result = await _authService.GetRolesAsync(query);
                return Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // 13. EGYEDI SZEREPKÖR LEKÉRDEZÉSE (visszatérési érték javítva!)
        [HttpGet("roles/{id:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetSpecificRole(Guid id)
        {
            try
            {
                var result = await _authService.GetSpecificRoleAsync(id);
                return Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
            catch (Exception ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        // 14. SZEREPKÖR LÉTREHOZÁSA
        [HttpPost("roles")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> CreateRole([FromBody] CreateRoleDto dto)
        {
            try
            {
                var result = await _authService.CreateRoleAsync(dto);
                return CreatedAtAction(nameof(GetSpecificRole), new { id = result.Id }, result);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // 15. SZEREPKÖR MÓDOSÍTÁSA
        [HttpPatch("roles/{id:guid}/modify")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ModifyRole(Guid id, [FromBody] ModifyRoleDto dto)
        {
            try
            {
                var result = await _authService.ModifyRoleAsync(id, dto);
                return Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
            catch (Exception ex) when (ex.Message.Contains("nem található"))
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // 16. SZEREPKÖR TÖRLÉSE
        [HttpDelete("roles/{id:guid}/remove")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RemoveRole(Guid id)
        {
            try
            {
                await _authService.RemoveRoleAsync(id);
                return NoContent();
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
            catch (Exception ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        // 17. JOGOSULTSÁG HOZZÁRENDELÉSE SZEREPKÖRHÖZ
        [HttpPost("roles/{id:guid}/permission")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AddPermToRole(Guid id, [FromBody] AddPermToRoletDto dto)
        {
            try
            {
                var result = await _authService.AddPermissionToRoleAsync(id, dto);
                return Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
            catch (Exception ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        // 18. EGYEDI MUNKAMENET LEZÁRÁSA (dupla "auth/" útvonal javítva!)
        [HttpDelete("sessions/{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CloseSession(Guid id)
        {
            try
            {
                await _authService.CloseSessionAsync(id);
                return NoContent();
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
            catch (Exception ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        // 19. ÖSSZES MUNKAMENET LEZÁRÁSA (dupla "auth/" útvonal javítva!)
        [HttpDelete("sessions")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CloseSessions()
        {
            try
            {
                await _authService.CloseSessionsAsync();
                return NoContent();
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Váratlan hiba történt az összes munkamenet visszavonásakor.");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Szerveroldali hiba történt." });
            }
        }

        // 20. MUNKAMENETEK LISTÁZÁSA (lapozás validálva!)
        [HttpGet("sessions")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetUserSessions([FromQuery] int page = 1, [FromQuery] int limit = 20)
        {
            if (page < 1 || limit < 1 || limit > 100)
            {
                return BadRequest(new { message = "Érvénytelen lapozási paraméterek (page >= 1, 1 <= limit <= 100)." });
            }

            try
            {
                var result = await _authService.GetSessionsAsync(page, limit);
                return Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Váratlan hiba történt a munkamenetek lekérdezésekor.");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Szerveroldali hiba történt." });
            }
        }

        // 21. JELSZÓ-VISSZAÁLLÍTÁS KÉRÉSE ([AllowAnonymous] hozzáadva!)
        [AllowAnonymous]
        [HttpPost("password-reset")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> RequestPasswordReset([FromQuery] string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return BadRequest(new { message = "Az e-mail cím megadása kötelező." });
            }

            try
            {
                await _authService.RequestPasswordResetAsync(email);
                return Ok(new { message = "Ha az e-mail cím szerepel a rendszerben, elküldtük a visszaállító hivatkozást." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Váratlan hiba történt a jelszó-visszaállítási kérelem feldolgozásakor.");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Szerveroldali hiba történt." });
            }
        }

        // 22. JELSZÓ-VISSZAÁLLÍTÁS VÉGREHAJTÁSA ([AllowAnonymous] és bool ellenőrzés hozzáadva!)
        [AllowAnonymous]
        [HttpPut("password-reset")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> PasswordReset([FromQuery] string email, [FromQuery] string token, [FromBody] ResetPwdDto dto)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token))
            {
                return BadRequest(new { message = "Az e-mail cím és a token megadása kötelező." });
            }

            try
            {
                var success = await _authService.ResetPasswordAsync(email, token, dto);
                if (!success)
                {
                    return BadRequest(new { message = "A visszaállító hivatkozás érvénytelen vagy lejárt." });
                }

                return Ok(new { message = "A jelszó sikeresen megváltoztatva." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Váratlan hiba történt a jelszó visszaállítása során.");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Szerveroldali hiba történt." });
            }
        }
    }
}