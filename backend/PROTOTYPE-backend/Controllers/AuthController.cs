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
            var result = await _authService.CreateUserAsync(dto);
            return StatusCode(StatusCodes.Status201Created, result);
            
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
                return Unauthorized(new { error = new { message = ex.Message, code = "NINCS_HITELESITVE" } });
            }
        }

        // 3. 2FA BEÁLLÍTÁS KEZDÉSE (TOTP) - Saját fiók ([Authorize])
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
                return Unauthorized(new { error = new { message = ex.Message, code = "NINCS_HITELESITVE" } });
            }
        }

        // 4. 2FA ENGEDÉLYEZÉSE / VERIFIKÁCIÓJA SETUP UTÁN (TOTP) - Saját fiók ([Authorize])
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
                return Unauthorized(new { error = new { message = ex.Message, code = "NINCS_HITELESITVE" } });
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
                return Unauthorized(new { error = new { message = ex.Message, code = "NINCS_HITELESITVE" } });
            }
        }

        // E-mailes 2FA kód újraküldése
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
                return Unauthorized(new { error = new { message = ex.Message, code = "NINCS_HITELESITVE" } });
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("30 másodperc"))
            {
                return StatusCode(StatusCodes.Status429TooManyRequests, new { error = new { message = ex.Message, code = "TUL_SOK_KERES" } });
            }
        }

        // 6. 2FA KIKAPCSOLÁSA - Saját fiók ([Authorize])
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
                return Unauthorized(new { error = new { message = ex.Message, code = "NINCS_HITELESITVE" } });
            }
        }

        // 7. E-MAILES 2FA ENGEDÉLYEZÉSE - Saját fiók ([Authorize])
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
                return Unauthorized(new { error = new { message = ex.Message, code = "NINCS_HITELESITVE" } });
            }
        }

        // 8. HELYREÁLLÍTÓ KÓDOK ÚJRAGENERÁLÁSA - Saját fiók ([Authorize])
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
                return Unauthorized(new { error = new { message = ex.Message, code = "NINCS_HITELESITVE" } });
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
                return BadRequest(new { error = new { message = "A refresh token megadása kötelező.", code = "ERVENYTELEN_KERES" } });
            }

            try
            {
                var result = await _authService.RefreshAsync(dto.RefreshToken);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = new { message = ex.Message, code = "NINCS_HITELESITVE" } });
            }
        }

        // 10. KIJELENTKEZÉS - Saját munkamenet ([Authorize])
        [HttpPost("logout")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Logout([FromBody] RefreshTokenRequestDto dto)
        {
            var userId = User.GetUserId();
            await _authService.LogoutAsync(userId, dto.RefreshToken);
            return NoContent();
            
        }

        // 11. JOGOSULTSÁGOK KATEGÓRIA SZERINT
        [HttpGet("permissions")]
        [Authorize(Policy = "workspace.roles.manage")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetPermissions([FromQuery] string query)
        {
            var result = await _authService.GetPermissionsByCatAsync(query);
            return Ok(result);
            
        }

        // 12. SZEREPKÖRÖK LISTÁZÁSA
        [HttpGet("roles")]
        [Authorize(Policy = "workspace.roles.manage")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetRoles([FromQuery] string query = "")
        {
            var result = await _authService.GetRolesAsync(query);
            return Ok(result);
            
        }

        // 13. EGYEDI SZEREPKÖR LEKÉRDEZÉSE
        [HttpGet("roles/{id:guid}")]
        [Authorize(Policy = "workspace.roles.manage")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetSpecificRole(Guid id)
        {
            var result = await _authService.GetSpecificRoleAsync(id);
            return Ok(result);
            
        }

        // 14. SZEREPKÖR LÉTREHOZÁSA
        [HttpPost("roles")]
        [Authorize(Policy = "system.roles.manage")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CreateRole([FromBody] CreateRoleDto dto)
        {
            var result = await _authService.CreateRoleAsync(dto);
            return CreatedAtAction(nameof(GetSpecificRole), new { id = result.Id }, result);
            
        }

        // 15. SZEREPKÖR MÓDOSÍTÁSA
        [HttpPatch("roles/{id:guid}/modify")]
        [Authorize(Policy = "system.roles.manage")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ModifyRole(Guid id, [FromBody] ModifyRoleDto dto)
        {
            var result = await _authService.ModifyRoleAsync(id, dto);
            return Ok(result);
            
        }

        // 16. SZEREPKÖR TÖRLÉSE
        [HttpDelete("roles/{id:guid}/remove")]
        [Authorize(Policy = "system.roles.manage")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RemoveRole(Guid id)
        {
            await _authService.RemoveRoleAsync(id);
            return NoContent();
            
        }

        // 17. JOGOSULTSÁG HOZZÁRENDELÉSE SZEREPKÖRHÖZ
        [HttpPost("roles/{id:guid}/permission")]
        [Authorize(Policy = "system.roles.manage")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AddPermToRole(Guid id, [FromBody] AddPermToRoletDto dto)
        {
            var result = await _authService.AddPermissionToRoleAsync(id, dto);
            return Ok(result);
            
        }

        // 18. EGYEDI MUNKAMENET LEZÁRÁSA
        [HttpDelete("sessions/{id:guid}")]
        [Authorize(Policy = "system.users.manage")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CloseSession(Guid id)
        {
            await _authService.CloseSessionAsync(id);
            return NoContent();
            
        }

        // 19. ÖSSZES MUNKAMENET LEZÁRÁSA
        [HttpDelete("sessions")]
        [Authorize(Policy = "system.users.manage")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CloseSessions()
        {
            await _authService.CloseSessionsAsync();
            return NoContent();
            
        }

        // 20. MUNKAMENETEK LISTÁZÁSA
        [HttpGet("sessions")]
        [Authorize(Policy = "system.users.manage")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetUserSessions([FromQuery] int page = 1, [FromQuery] int limit = 20)
        {
            if (page < 1 || limit < 1 || limit > 100)
            {
                return BadRequest(new { error = new { message = "Érvénytelen lapozási paraméterek (page >= 1, 1 <= limit <= 100).", code = "ERVENYTELEN_KERES" } });
            }

            var result = await _authService.GetSessionsAsync(page, limit);
            return Ok(result);
            
        }

        // 21. JELSZÓ-VISSZAÁLLÍTÁS KÉRÉSE
        [AllowAnonymous]
        [HttpPost("password-reset")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> RequestPasswordReset([FromQuery] string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return BadRequest(new { error = new { message = "Az e-mail cím megadása kötelező.", code = "ERVENYTELEN_KERES" } });
            }

            await _authService.RequestPasswordResetAsync(email);
            return Ok(new { message = "Ha az e-mail cím szerepel a rendszerben, elküldtük a visszaállító hivatkozást." });
            
        }

        // 22. JELSZÓ-VISSZAÁLLÍTÁS VÉGREHAJTÁSA
        [AllowAnonymous]
        [HttpPut("password-reset")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> PasswordReset([FromQuery] string email, [FromQuery] string token, [FromBody] ResetPwdDto dto)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token))
            {
                return BadRequest(new { error = new { message = "Az e-mail cím és a token megadása kötelező.", code = "ERVENYTELEN_KERES" } });
            }

            var success = await _authService.ResetPasswordAsync(email, token, dto);
            if (!success)
            {
                return BadRequest(new { error = new { message = "A visszaállító hivatkozás érvénytelen vagy lejárt.", code = "ERVENYTELEN_KERES" } });
            }

            return Ok(new { message = "A jelszó sikeresen megváltoztatva." });
            
        }
    }
}