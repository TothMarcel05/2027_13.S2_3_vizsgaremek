using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        // GET /api/users/me -> Saját adatok lekérése
        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentUserProfile()
        {
            var userId = User.GetUserId();
            var profile = await _userService.GetUserProfileAsync(userId);

            if (profile == null)
            {
                return NotFound(new { message = "A felhasználó nem található." });
            }

            return Ok(profile);
        }

        // PUT /api/users/me -> Saját adatok frissítése
        [HttpPut("me")]
        public async Task<IActionResult> UpdateCurrentUserProfile([FromBody] UserModifyRequestDto dto)
        {
            var userId = User.GetUserId();
            var updatedProfile = await _userService.UpdateUserProfileAsync(userId, dto);

            if (updatedProfile == null)
            {
                return NotFound(new { message = "A felhasználó nem található." });
            }

            return Ok(updatedProfile);
        }

        // POST /api/users/me/change-password -> Jelszó módosítása
        [HttpPost("me/change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDto dto)
        {
            var userId = User.GetUserId();
            var success = await _userService.ChangePasswordAsync(userId, dto);

            if (!success)
            {
                return BadRequest(new { message = "A jelenlegi jelszó hibás." });
            }

            return NoContent(); // 204 No Content
        }

        // DELETE /api/users/me -> Saját fiók törlése (Soft delete)
        [HttpDelete("me")]
        public async Task<IActionResult> DeleteCurrentUserAccount()
        {
            var userId = User.GetUserId();
            var result = await _userService.DeleteUserAccountAsync(userId);

            if (result == null)
            {
                return NotFound(new { message = "A felhasználó nem található." });
            }

            return Ok(result);
        }

        // GET /api/users/{id} -> Egy adott csapattag publikus profilja
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetPublicProfile(Guid id)
        {
            var publicProfile = await _userService.GetPublicProfileByIdAsync(id);

            if (publicProfile == null)
            {
                return NotFound(new { message = "A felhasználó nem található." });
            }

            return Ok(publicProfile);
        }

        // GET /api/users/search?q=kovacs -> Csapattagok keresése
        [HttpGet("search")]
        public async Task<IActionResult> SearchUsers([FromQuery] string q)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 2)
            {
                return BadRequest(new { message = "A keresési kifejezésnek legalább 2 karakterből kell állnia." });
            }

            var users = await _userService.SearchUsersAsync(q);
            return Ok(users);
        }
    }
}