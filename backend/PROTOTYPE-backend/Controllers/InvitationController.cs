using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<InvitationController> _logger;

        public InvitationController(IInvitationService invitationService, ILogger<InvitationController> logger)
        {
            _invitationService = invitationService;
            _logger = logger;
        }

        // 1. SAJÁT MEGHÍVÓK LEKÉRÉSE
        [HttpGet("my")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetMyInvitations()
        {
            try
            {
                var userId = User.GetUserId();
                var result = await _invitationService.GetMyInvitationsAsync(userId);
                return Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(new { message = "Érvénytelen vagy lejárt hitelesítés." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Váratlan hiba történt a saját meghívók lekérdezésekor.");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Szerveroldali hiba történt a meghívók lekérdezésekor." });
            }
        }

        // 2. CÉL (WORKSPACE / PROJECT) FÜGGŐBEN LÉVŐ MEGHÍVÓI
        [HttpGet("target/{targetId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetPendingByTarget(Guid targetId, [FromQuery] sbyte type)
        {
            if (targetId == Guid.Empty)
            {
                return BadRequest(new { message = "A 'targetId' megadása kötelező." });
            }

            if (type != 1 && type != 2)
            {
                return BadRequest(new { message = "Érvénytelen meghívó típus (1 = Workspace, 2 = Project)." });
            }

            try
            {
                var result = await _invitationService.GetPendingInvitationsByTargetAsync(targetId, type);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Váratlan hiba történt a függőben lévő meghívók lekérdezésekor. TargetId: {TargetId}, Type: {Type}", targetId, type);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Szerveroldali hiba történt a meghívók lekérdezésekor." });
            }
        }

        // 3. MEGHÍVÓ LÉTREHOZÁSA
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateInvitation([FromBody] CreateInvitationDto dto)
        {
            if (dto.TargetId == Guid.Empty || string.IsNullOrWhiteSpace(dto.InvitedEmail))
            {
                return BadRequest(new { message = "A cél azonosító (TargetId) és a meghívott e-mail címe kötelező." });
            }

            if (dto.Type != 1 && dto.Type != 2)
            {
                return BadRequest(new { message = "Érvénytelen meghívó típus (1 = Workspace, 2 = Project)." });
            }

            try
            {
                var userId = User.GetUserId();
                var result = await _invitationService.CreateInvitationAsync(userId, dto);
                return CreatedAtAction(nameof(GetByToken), new { token = result.Token }, result);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(new { message = "Érvénytelen vagy lejárt hitelesítés." });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Váratlan hiba történt a meghívó létrehozásakor. TargetId: {TargetId}", dto.TargetId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Szerveroldali hiba történt a meghívó létrehozásakor." });
            }
        }

        // 4. MEGHÍVÓ ELŐNÉZETE TOKEN ALAPJÁN (PUBLIKUS)
        [AllowAnonymous]
        [HttpGet("token/{token}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetByToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return BadRequest(new { message = "A meghívó token megadása kötelező." });
            }

            try
            {
                var result = await _invitationService.GetInvitationByTokenAsync(token);
                if (result == null)
                {
                    return NotFound(new { message = "A meghívó nem található." });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Váratlan hiba történt a meghívó előnézetének lekérdezésekor.");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Szerveroldali hiba történt a meghívó lekérdezésekor." });
            }
        }

        // 5. MEGHÍVÓ ELFOGADÁSA
        [HttpPost("accept/{token}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> AcceptInvitation(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return BadRequest(new { message = "A meghívó token megadása kötelező." });
            }

            try
            {
                var userId = User.GetUserId();
                var success = await _invitationService.AcceptInvitationAsync(userId, token);
                if (!success)
                {
                    return BadRequest(new { message = "A meghívó érvénytelen, lejárt vagy már felhasználták." });
                }

                return Ok(new { message = "Sikeresen csatlakoztál!" });
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(new { message = "Érvénytelen vagy lejárt hitelesítés." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Váratlan hiba történt a meghívó elfogadásakor.");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Szerveroldali hiba történt a meghívó elfogadásakor." });
            }
        }

        // 6. MEGHÍVÓ ELUTASÍTÁSA
        [HttpPost("refuse/{token}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> RefuseInvitation(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return BadRequest(new { message = "A meghívó token megadása kötelező." });
            }

            try
            {
                var userId = User.GetUserId();
                var success = await _invitationService.RefuseInvitationAsync(userId, token);
                if (!success)
                {
                    return BadRequest(new { message = "A meghívó érvénytelen vagy már feldolgozták." });
                }

                return Ok(new { message = "Meghívó elutasítva." });
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(new { message = "Érvénytelen vagy lejárt hitelesítés." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Váratlan hiba történt a meghívó elutasításakor.");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Szerveroldali hiba történt a meghívó elutasításakor." });
            }
        }

        // 7. MEGHÍVÓ VISSZAVONÁSA / TÖRLÉSE
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CancelInvitation(Guid id)
        {
            try
            {
                var userId = User.GetUserId();
                var success = await _invitationService.CancelInvitationAsync(userId, id);
                if (!success)
                {
                    return NotFound(new { message = "A meghívó nem található." });
                }

                return NoContent();
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(new { message = "Érvénytelen vagy lejárt hitelesítés." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Váratlan hiba történt a meghívó visszavonásakor. InvitationId: {InvitationId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Szerveroldali hiba történt a meghívó visszavonásakor." });
            }
        }
    }
}