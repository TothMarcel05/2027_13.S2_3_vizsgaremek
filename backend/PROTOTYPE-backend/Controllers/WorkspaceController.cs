using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PROTOTYPE_backend.DTOs.Auth;
using PROTOTYPE_backend.DTOs.Workspace;
using PROTOTYPE_backend.Extensions;
using PROTOTYPE_backend.Services.Workspaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PROTOTYPE_backend.Controllers
{
    [ApiController]
    [Route("api/workspaces")]
    [Authorize]
    public class WorkspaceController : ControllerBase
    {
        private readonly IWorkspaceService _workspaceService;
        private readonly ILogger<WorkspaceController> _logger;

        public WorkspaceController(IWorkspaceService workspaceService, ILogger<WorkspaceController> logger)
        {
            _workspaceService = workspaceService;
            _logger = logger;
        }

        // 1. SAJÁT MUNKATERÜLETEK LISTÁZÁSA ([Authorize])
        [HttpGet("my")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetMyWorkspaces([FromQuery] int page = 1, [FromQuery] int limit = 20)
        {
            if (page < 1 || limit < 1 || limit > 100)
            {
                return BadRequest(new { message = "Érvénytelen lapozási paraméterek (page >= 1, 1 <= limit <= 100)." });
            }

            try
            {
                var userId = User.GetUserId();
                var workspaces = await _workspaceService.GetUserWorkspacesAsync(userId, page, limit);
                return Ok(workspaces);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(new { message = "Érvénytelen vagy lejárt hitelesítés." });
            }
            catch (Exception ex) when (ex.Message.Contains("Nem létezik"))
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Váratlan hiba történt a felhasználó munkaterületeinek lekérdezésekor.");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Szerveroldali hiba történt a munkaterületek lekérdezésekor." });
            }
        }

        // 2. EGYEDI MUNKATERÜLET ADATAI ([Authorize])
        [HttpGet("{workspaceId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetWorkspaceData(Guid workspaceId)
        {
            try
            {
                var workspace = await _workspaceService.GetWorkspaceDataAsync(workspaceId);
                if (workspace == null)
                {
                    return NotFound(new { message = "A megadott munkaterület nem található." });
                }

                return Ok(workspace);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Váratlan hiba történt a munkaterület adatainak lekérdezésekor. WorkspaceId: {WorkspaceId}", workspaceId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Szerveroldali hiba történt a munkaterület lekérdezésekor." });
            }
        }

        // 3. MUNKATERÜLET TAGJAINAK LISTÁZÁSA ([Authorize])
        [HttpGet("{workspaceId:guid}/members")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetWorkspaceMembers(
            Guid workspaceId,
            [FromQuery] int page = 1,
            [FromQuery] int limit = 50,
            [FromQuery] Guid? role = null)
        {
            if (page < 1 || limit < 1 || limit > 100)
            {
                return BadRequest(new { message = "Érvénytelen lapozási paraméterek (page >= 1, 1 <= limit <= 100)." });
            }

            try
            {
                var members = await _workspaceService.GetWorkspaceMembersAsync(workspaceId, page, limit, role);
                return Ok(members);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Váratlan hiba történt a munkaterület tagjainak lekérdezésekor. WorkspaceId: {WorkspaceId}", workspaceId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Szerveroldali hiba történt a tagok lekérdezésekor." });
            }
        }

        // 4. MUNKATERÜLET TÁRHELY-HASZNÁLATA ([Authorize])
        [HttpGet("{workspaceId:guid}/storage")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetWorkspaceStorage(Guid workspaceId)
        {
            try
            {
                var storage = await _workspaceService.GetWorkspaceStorageAsync(workspaceId);
                if (storage == null)
                {
                    return NotFound(new { message = "A megadott munkaterület nem található." });
                }

                return Ok(storage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Váratlan hiba történt a tárhely-használat lekérdezésekor. WorkspaceId: {WorkspaceId}", workspaceId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Szerveroldali hiba történt a tárhely adatok lekérdezésekor." });
            }
        }

        // 5. MUNKATERÜLET PROJEKTJEINEK LISTÁZÁSA ([Authorize])
        [HttpGet("{workspaceId:guid}/projects")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetWorkspaceProjects(
            Guid workspaceId,
            [FromQuery] int page = 1,
            [FromQuery] int limit = 20,
            [FromQuery] sbyte? status = null)
        {
            if (page < 1 || limit < 1 || limit > 100)
            {
                return BadRequest(new { message = "Érvénytelen lapozási paraméterek (page >= 1, 1 <= limit <= 100)." });
            }

            try
            {
                var projects = await _workspaceService.GetWorkspaceProjectsAsync(workspaceId, page, limit, status);
                return Ok(projects);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Váratlan hiba történt a projektek lekérdezésekor. WorkspaceId: {WorkspaceId}", workspaceId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Szerveroldali hiba történt a projektek lekérdezésekor." });
            }
        }

        // 6. ÚJ MUNKATERÜLET LÉTREHOZÁSA ([Authorize])
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateWorkspace([FromBody] CreateWorkspaceDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.workspaceName))
            {
                return BadRequest(new { message = "A munkaterület neve nem lehet üres." });
            }

            try
            {
                var userId = User.GetUserId();
                var workspace = await _workspaceService.CreateWorkspaceAsync(userId, dto);
                return CreatedAtAction(nameof(GetWorkspaceData), new { workspaceId = workspace.workspaceId }, workspace);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(new { message = "Érvénytelen vagy lejárt hitelesítés." });
            }
            catch (Exception ex) when (ex.Message.Contains("Már létezik"))
            {
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Váratlan hiba történt új munkaterület létrehozásakor.");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Szerveroldali hiba történt a munkaterület létrehozásakor." });
            }
        }

        // 7. ÚJ PROJEKT LÉTREHOZÁSA A MUNKATERÜLETEN BELÜL
        [HttpPost("{workspaceId:guid}/project")]
        [Authorize(Policy = "project.create")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateProject(Guid workspaceId, [FromBody] CreateProjectDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.name))
            {
                return BadRequest(new { message = "A projekt neve nem lehet üres." });
            }

            try
            {
                var project = await _workspaceService.CreateProjectAsync(workspaceId, dto);
                return StatusCode(StatusCodes.Status201Created, project);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Váratlan hiba történt új projekt létrehozásakor. WorkspaceId: {WorkspaceId}", workspaceId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Szerveroldali hiba történt a projekt létrehozásakor." });
            }
        }

        // 8. MUNKATERÜLET-VÁLTÁS ([Authorize])
        [HttpPost("{workspaceId:guid}/switch")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<WorkspaceTokenDto>> SwitchWorkspace(Guid workspaceId)
        {
            try
            {
                var userId = User.GetUserId();
                var token = await _workspaceService.SwitchWorkspaceAsync(userId, workspaceId);
                return Ok(new WorkspaceTokenDto(token));
            }
            catch (UnauthorizedAccessException ex) when (ex.Message.Contains("hozzáférésed"))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Váratlan hiba történt a munkaterület-váltás során. WorkspaceId: {WorkspaceId}", workspaceId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Szerveroldali hiba történt a munkaterület-váltáskor." });
            }
        }

        // 9. MUNKATERÜLET TÖRLÉSE
        [HttpDelete("{workspaceId:guid}")]
        [Authorize(Policy = "workspace.manage")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteWorkspace(Guid workspaceId)
        {
            try
            {
                var result = await _workspaceService.DeleteWorkspaceAsync(workspaceId);
                if (!result)
                {
                    return NotFound(new { message = "A megadott munkaterület nem található." });
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Váratlan hiba történt a munkaterület törlésekor. WorkspaceId: {WorkspaceId}", workspaceId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Szerveroldali hiba történt a munkaterület törlésekor." });
            }
        }

        // 10. MUNKATERÜLET ADATAINAK MÓDOSÍTÁSA
        [HttpPut("{workspaceId:guid}")]
        [Authorize(Policy = "workspace.manage")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateWorkspace(Guid workspaceId, [FromBody] WorkspaceDataChangeDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.workspaceName))
            {
                return BadRequest(new { message = "A munkaterület neve nem lehet üres." });
            }

            try
            {
                var result = await _workspaceService.UpdateWorkspaceAsync(workspaceId, dto);
                if (result == null)
                {
                    return NotFound(new { message = "A megadott munkaterület nem található." });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Váratlan hiba történt a munkaterület módosításakor. WorkspaceId: {WorkspaceId}", workspaceId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Szerveroldali hiba történt a munkaterület frissítésekor." });
            }
        }

        // 11. TULAJDONOSVÁLTÁS
        [HttpPut("{workspaceId:guid}/owner")]
        [Authorize(Policy = "workspace.ownership.transfer")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ChangeWorkspaceOwner(Guid workspaceId, [FromBody] WorkspaceOwnerChangeDto dto)
        {
            if (dto.newOwnerId == Guid.Empty)
            {
                return BadRequest(new { message = "Az új tulajdonos azonosítójának megadása kötelező." });
            }

            try
            {
                var result = await _workspaceService.ChangeWorkspaceOwnerAsync(workspaceId, dto);
                if (result == null)
                {
                    return NotFound(new { message = "A megadott munkaterület nem található." });
                }

                return Ok(result);
            }
            catch (Exception ex) when (ex.Message.Contains("nem található"))
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Váratlan hiba történt a tulajdonosváltás során. WorkspaceId: {WorkspaceId}", workspaceId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Szerveroldali hiba történt a tulajdonosváltáskor." });
            }
        }

        // 12. TAG SZEREPKÖRÉNEK MÓDOSÍTÁSA
        [HttpPut("{workspaceId:guid}/members/{userId:guid}")]
        [Authorize(Policy = "workspace.members.manage")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateMemberRole(Guid workspaceId, Guid userId, [FromBody] WorkspaceMemberRoleChangeDto dto)
        {
            try
            {
                var result = await _workspaceService.UpdateMemberRoleAsync(workspaceId, userId, dto);
                if (result == null)
                {
                    return NotFound(new { message = "A tag vagy munkaterület nem található." });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Váratlan hiba történt a tag szerepkörének módosításakor. WorkspaceId: {WorkspaceId}, UserId: {UserId}",
                    workspaceId, userId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Szerveroldali hiba történt a szerepkör módosításakor." });
            }
        }

        // 13. TAG ELTÁVOLÍTÁSA A MUNKATERÜLETRŐL
        [HttpDelete("{workspaceId:guid}/members/{userId:guid}")]
        [Authorize(Policy = "workspace.members.manage")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> RemoveMember(Guid workspaceId, Guid userId)
        {
            try
            {
                var result = await _workspaceService.RemoveMemberAsync(workspaceId, userId);
                if (!result)
                {
                    return NotFound(new { message = "A tag vagy munkaterület nem található." });
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Váratlan hiba történt a tag eltávolításakor. WorkspaceId: {WorkspaceId}, UserId: {UserId}",
                    workspaceId, userId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Szerveroldali hiba történt a tag eltávolításakor." });
            }
        }

        // 14. FELADAT LEFOGLALÁSA
        [HttpPost("tasks/reserve")]
        [Authorize(Policy = "task.reserve")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ReserveTask([FromBody] ReserveTaskDto dto)
        {
            try
            {
                var currentUserId = User.GetUserId();
                var secureDto = dto with { assigneeId = currentUserId };

                var result = await _workspaceService.ReserveTaskAsync(secureDto);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                // A feladat már hozzá van rendelve valakihez
                return Conflict(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                // Hiányzó projekttagság vagy szerepkör
                return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Váratlan hiba történt a feladat lefoglalása során. TaskId: {TaskId}, ProjectId: {ProjectId}",
                    dto.id, dto.projectId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Szerveroldali hiba történt a feladat lefoglalásakor." });
            }
        }

        // 15. FELADAT LEFOGLALÁSÁNAK LEMONDÁSA
        [HttpPost("tasks/unreserve")]
        [Authorize(Policy = "task.reserve")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UnReserveTask([FromBody] ReserveTaskDto dto)
        {
            try
            {
                var currentUserId = User.GetUserId();
                var secureDto = dto with { assigneeId = currentUserId };

                await _workspaceService.UnReserveTaskAsync(secureDto);
                return Ok(new { message = "A feladat lefoglalása sikeresen lemondva." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                // A feladat még nincs lefoglalva
                return Conflict(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                // Más által lefoglalt feladat vagy hiányzó szerepkör
                return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Váratlan hiba történt a feladat lefoglalásának lemondásakor. TaskId: {TaskId}, ProjectId: {ProjectId}",
                    dto.id, dto.projectId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Szerveroldali hiba történt a feladat lemondásakor." });
            }
        }
    }
}