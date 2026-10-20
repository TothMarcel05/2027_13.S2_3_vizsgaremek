// KM

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PROTOTYPE_backend.DTOs.Auth;
using PROTOTYPE_backend.Services.Auth;
using System.Security.Claims;

namespace PROTOTYPE_backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService) 
        {
            _authService = authService;
        }
        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto) 
        {
            var result = await _authService.CreateUserAsync(dto);

            return CreatedAtAction(nameof(Register), new { id = result.Id }, result);
        }
        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto) 
        {
            var result = await _authService.LoginAsync(dto);

            return CreatedAtAction(nameof(Login), new { id = result.Auth?.User.Id }, result);
        }
    }
}
