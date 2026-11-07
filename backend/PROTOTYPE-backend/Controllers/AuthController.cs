using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        // 1. REGISZTRÁCIÓ
        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            try
            {
                var result = await _authService.CreateUserAsync(dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // 2. BEJELENTKEZÉS
        [AllowAnonymous]
        [HttpPost("login")]
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
        }

        // 3. 2FA BEÁLLÍTÁS KEZDÉSE
        [HttpPost("2fa/setup")]
        public async Task<IActionResult> TwoFactorSetup()
        {
            try
            {
                var userId = User.GetUserId();
                var result = await _authService.BeginTwoFactorSetupAsync(userId);
                return Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // 4. 2FA ENGEDÉLYEZÉSE / VERIFIKÁCIÓJA SETUP UTÁN
        [HttpPost("2fa/enable")]
        public async Task<IActionResult> EnableTwoFactor([FromBody] TwoFactorCodeDto dto)
        {
            try
            {
                var userId = User.GetUserId();
                var recoveryCodes = await _authService.EnableTwoFactorAsync(userId, dto.Code);
                return Ok(new { recoveryCodes });
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
            catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // 5. 2FA BEJELENTKEZÉS IGAZOLÁSA
        [AllowAnonymous]
        [HttpPost("2fa/verify")]
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
        }

        // 6. TOKEN FRISSÍTÉS
        [AllowAnonymous]
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequestDto dto)
        {
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

        // 7. KIJELENTKEZÉS
        [HttpPost("logout")]
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
        }
        [HttpGet("permissions")]
        public async Task<IActionResult> GetPermissions(string query) 
        {
            try
            {
                await _authService.GetPermissionsByCatAsync(query);
                return Ok();
            }
            catch (UnauthorizedAccessException) 
            {
                return Unauthorized();
            }
        }
        [HttpGet("roles")]
        public async Task<IActionResult> GetRoles(string query)
        {
            try
            {
                await _authService.GetRolesAsync(query);
                return Ok();
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
        }
        [HttpGet("roles/{id}")]
        public async Task<IActionResult> GetSpecificRole(Guid id)
        {
            try
            {
                await _authService.GetSpecificRoleAsync(id);
                return Ok();
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
        }
        [HttpPost("roles")]
        public async Task<IActionResult> CreateRole(CreateRoleDto dto)
        {
            try
            {
                var result = await _authService.CreateRoleAsync(dto);
                return Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
        }
        [HttpPatch("roles/{id}/modify")]
        public async Task<IActionResult> ModifyRole(Guid id, ModifyRoleDto dto)
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
        }
        [HttpDelete("roles/{id}/remove")]
        public async Task<IActionResult> RemoveRole(Guid id)
        {
            try
            {
                var result = await _authService.RemoveRoleAsync(id);
                return NoContent();
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
        }
        [HttpPost("roles/{id}/permission")]
        public async Task<IActionResult> AddPermToRole(Guid id, AddPermToRoletDto dto) 
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
        }
        [HttpDelete("auth/sessions/{id}")]
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
        }
        [HttpDelete("auth/sessions")]
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
        }
    }
}