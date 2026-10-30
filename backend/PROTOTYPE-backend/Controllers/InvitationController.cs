using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PROTOTYPE_backend.DTOs.Invitations;
using PROTOTYPE_backend.Extensions;
using PROTOTYPE_backend.Services.Invitations;

namespace PROTOTYPE_backend.Controllers
{
    [ApiController]
    [Route("api/invitations")]
    public class InvitationController : ControllerBase
    {
        private readonly IInvitationService _invitationService;

        public InvitationController(IInvitationService invitationService)
        {
            _invitationService = invitationService;
        }

        // POST /api/invitations -> Új meghívó kiküldése
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreateInvitation([FromBody] CreateInvitationDto dto)
        {
            var userId = User.GetUserId();
            var result = await _invitationService.CreateInvitationAsync(userId, dto);
            return Ok(result);
        }

        // GET /api/invitations/my -> Saját kapott meghívók
        [Authorize]
        [HttpGet("my")]
        public async Task<IActionResult> GetMyInvitations()
        {
            var userId = User.GetUserId();
            var result = await _invitationService.GetMyInvitationsAsync(userId);
            return Ok(result);
        }

        // GET /api/invitations/target/{targetId:guid}?type=1 -> Adott célpont függőben lévő meghívói
        [Authorize]
        [HttpGet("target/{targetId:guid}")]
        public async Task<IActionResult> GetPendingByTarget(Guid targetId, [FromQuery] sbyte type)
        {
            var result = await _invitationService.GetPendingInvitationsByTargetAsync(targetId, type);
            return Ok(result);
        }

        // GET /api/invitations/token/{token} -> Meghívó ellenőrzése token alapján (Akár anonim is)
        [AllowAnonymous]
        [HttpGet("token/{token}")]
        public async Task<IActionResult> GetByToken(string token)
        {
            var result = await _invitationService.GetInvitationByTokenAsync(token);
            if (result == null) return NotFound(new { message = "A meghívó nem található." });
            return Ok(result);
        }

        // POST /api/invitations/accept/{token} -> Meghívó elfogadása
        [Authorize]
        [HttpPost("accept/{token}")]
        public async Task<IActionResult> AcceptInvitation(string token)
        {
            var userId = User.GetUserId();
            var success = await _invitationService.AcceptInvitationAsync(userId, token);
            if (!success) return BadRequest(new { message = "A meghívó érvénytelen vagy már felhasználták." });
            return Ok(new { message = "Sikeresen csatlakoztál!" });
        }

        // POST /api/invitations/refuse/{token} -> Meghívó elutasítása
        [Authorize]
        [HttpPost("refuse/{token}")]
        public async Task<IActionResult> RefuseInvitation(string token)
        {
            var userId = User.GetUserId();
            var success = await _invitationService.RefuseInvitationAsync(userId, token);
            if (!success) return BadRequest(new { message = "A meghívó érvénytelen." });
            return Ok(new { message = "Meghívó elutasítva." });
        }

        // DELETE /api/invitations/{id:guid} -> Meghívó visszavonása
        [Authorize]
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