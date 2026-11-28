using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PROTOTYPE_backend.Authorization;
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
        private readonly IAuthorizationService _authorizationService;

        public InvitationController(IInvitationService invitationService, ILogger<InvitationController> logger,
            IAuthorizationService authorizationService)
        {
            _invitationService = invitationService;
            _logger = logger;
            _authorizationService = authorizationService;
        }

        // Az "invitation.manage" jogosultságot a meghívó céljára kell ellenőrizni, nem az aktív munkaterületre.
        // Nem létező meghívónál (null) az általános ellenőrzés fut, így a jogosult hívó 404-et kap.
        private async Task EnsureCanManageInvitationsAsync(InvitationTargetResource? target)
        {
            var authorization = await _authorizationService.AuthorizeAsync(User, target, "invitation.manage");
            if (!authorization.Succeeded)
                throw new UnauthorizedAccessException("Nincs jogosultságod a művelet végrehajtásához.");
        }

        // 1. SAJÁT MEGHÍVÓK LEKÉRÉSE ([Authorize])
        [HttpGet("my")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetMyInvitations()
        {
            var userId = User.GetUserId();
            var result = await _invitationService.GetMyInvitationsAsync(userId);
            return Ok(result);
            
        }

        // 2. CÉL (WORKSPACE / PROJECT) FÜGGŐBEN LÉVŐ MEGHÍVÓI
        [HttpGet("target/{targetId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetPendingByTarget(Guid targetId, [FromQuery] sbyte type)
        {
            if (targetId == Guid.Empty)
            {
                return BadRequest(new { error = new { message = "A 'targetId' megadása kötelező.", code = "ERVENYTELEN_KERES" } });
            }

            if (type != 1 && type != 2)
            {
                return BadRequest(new { error = new { message = "Érvénytelen meghívó típus (1 = Workspace, 2 = Project).", code = "ERVENYTELEN_KERES" } });
            }

            await EnsureCanManageInvitationsAsync(new InvitationTargetResource(type, targetId));

            var result = await _invitationService.GetPendingInvitationsByTargetAsync(targetId, type);
            return Ok(result);
            
        }

        // 3. MEGHÍVÓ LÉTREHOZÁSA
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateInvitation([FromBody] CreateInvitationDto dto)
        {
            if (dto.TargetId == Guid.Empty || string.IsNullOrWhiteSpace(dto.InvitedEmail))
            {
                return BadRequest(new { error = new { message = "A cél azonosító (TargetId) és a meghívott e-mail címe kötelező.", code = "ERVENYTELEN_KERES" } });
            }

            if (dto.Type != 1 && dto.Type != 2)
            {
                return BadRequest(new { error = new { message = "Érvénytelen meghívó típus (1 = Workspace, 2 = Project).", code = "ERVENYTELEN_KERES" } });
            }

            var target = new InvitationTargetResource(dto.Type, dto.TargetId);
            await EnsureCanManageInvitationsAsync(target);

            var userId = User.GetUserId();
            if (dto.RoleId.HasValue && !User.IsSystemAdmin())
                await _invitationService.EnsureCanGrantRoleAsync(userId, target, dto.RoleId.Value);

            var result = await _invitationService.CreateInvitationAsync(userId, dto);
            return CreatedAtAction(nameof(GetByToken), new { token = result.Token }, result);
            
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
                return BadRequest(new { error = new { message = "A meghívó token megadása kötelező.", code = "ERVENYTELEN_KERES" } });
            }

            var result = await _invitationService.GetInvitationByTokenAsync(token);
            if (result == null)
            {
                return NotFound(new { error = new { message = "A meghívó nem található.", code = "NEM_TALALHATO" } });
            }

            return Ok(result);
            
        }

        // 5. MEGHÍVÓ ELFOGADÁSA ([Authorize])
        [HttpPost("accept/{token}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> AcceptInvitation(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return BadRequest(new { error = new { message = "A meghívó token megadása kötelező.", code = "ERVENYTELEN_KERES" } });
            }

            var userId = User.GetUserId();
            var success = await _invitationService.AcceptInvitationAsync(userId, token);
            if (!success)
            {
                return BadRequest(new { error = new { message = "A meghívó érvénytelen, lejárt vagy már felhasználták.", code = "ERVENYTELEN_KERES" } });
            }

            return Ok(new { message = "Sikeresen csatlakoztál!" });
            
        }

        // 6. MEGHÍVÓ ELUTASÍTÁSA ([Authorize])
        [HttpPost("refuse/{token}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> RefuseInvitation(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return BadRequest(new { error = new { message = "A meghívó token megadása kötelező.", code = "ERVENYTELEN_KERES" } });
            }

            var userId = User.GetUserId();
            var success = await _invitationService.RefuseInvitationAsync(userId, token);
            if (!success)
            {
                return BadRequest(new { error = new { message = "A meghívó érvénytelen vagy már feldolgozták.", code = "ERVENYTELEN_KERES" } });
            }

            return Ok(new { message = "Meghívó elutasítva." });
            
        }

        // 7. MEGHÍVÓ VISSZAVONÁSA / TÖRLÉSE
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CancelInvitation(Guid id)
        {
            await EnsureCanManageInvitationsAsync(await _invitationService.GetInvitationTargetAsync(id));

            var userId = User.GetUserId();
            var success = await _invitationService.CancelInvitationAsync(userId, id);
            if (!success)
            {
                return NotFound(new { error = new { message = "A meghívó nem található.", code = "NEM_TALALHATO" } });
            }

            return NoContent();
            
        }
    }
}