using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PROTOTYPE_backend.DTOs.Users;
using PROTOTYPE_backend.Services.Users;
using System.Security.Claims;

namespace PROTOTYPE_backend.Controllers
{
    [ApiController]
    [Route("api/users")]
    //[Authorize]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService) 
        {
            _userService = userService;
        }

        [HttpGet("{userid}")]
        public async Task<IActionResult> GetUserInformation(Guid userId) 
        {
            var information = await _userService.UserInformationAsync(userId);

            if (information != null) 
            {
                return Ok(information);
            }

            return NotFound();
        }

        [HttpPut("{userid}")]
        public async Task<IActionResult> PutUserModification(Guid userid, [FromBody] UserModifyRequestDto dto) 
        {
            var result = await _userService.UserModifyAsync(userid, dto);

            if (result != null) 
            {
                return Ok(result);
            }

            return BadRequest(result);
        }

        [HttpDelete("{userid}")]
        public async Task<IActionResult> DeleteUser(Guid userid) 
        {
            var result = await _userService.UserDeleteAsync(userid);

            if (result != null) 
            {
                return Ok(result);
            }

            return BadRequest(result);
        }
    }
}
