using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PROTOTYPE_backend.DTOs.Invitations;
using PROTOTYPE_backend.Extensions;
using PROTOTYPE_backend.Services.Invitations;

namespace PROTOTYPE_backend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/invitations")]
    public class InvitationController : ControllerBase
    {
        private readonly IInvitationService _invitationService;

        public InvitationController(IInvitationService invitationService)
        {
            _invitationService = invitationService;
        }


        [HttpGet("my")]
        public async Task<IActionResult> GetMyInvitations()
        {
            var userId = User.GetUserId();
            var result = await _invitationService.GetMyInvitationsAsync(userId);
            return Ok(result);
        }

        [HttpGet("target/{targetId:guid}")]
        public async Task<IActionResult> GetPendingByTarget(Guid targetId, [FromQuery] sbyte type)
        {
            var result = await _invitationService.GetPendingInvitationsByTargetAsync(targetId, type);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> CreateInvitation([FromBody] CreateInvitationDto dto)
        {
            var userId = User.GetUserId();
            var result = await _invitationService.CreateInvitationAsync(userId, dto);
            return Ok(result);
        }

        [AllowAnonymous]
        [HttpGet("token/{token}")]
        public async Task<IActionResult> GetByToken(string token)
        {
            var result = await _invitationService.GetInvitationByTokenAsync(token);
            if (result == null) return NotFound(new { message = "A meghívó nem található." });
            return Ok(result);
        }

        [HttpPost("accept/{token}")]
        public async Task<IActionResult> AcceptInvitation(string token)
        {
            var userId = User.GetUserId();
            var success = await _invitationService.AcceptInvitationAsync(userId, token);
            if (!success) return BadRequest(new { message = "A meghívó érvénytelen vagy már felhasználták." });
            return Ok(new { message = "Sikeresen csatlakoztál!" });
        }

        [HttpPost("refuse/{token}")]
        public async Task<IActionResult> RefuseInvitation(string token)
        {
            var userId = User.GetUserId();
            var success = await _invitationService.RefuseInvitationAsync(userId, token);
            if (!success) return BadRequest(new { message = "A meghívó érvénytelen." });
            return Ok(new { message = "Meghívó elutasítva." });
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> CancelInvitation(Guid id)
        {
            var userId = User.GetUserId();
            var success = await _invitationService.CancelInvitationAsync(userId, id);
            if (!success) return NotFound(new { message = "A meghívó nem található." });
            return NoContent();
        }
    }
}