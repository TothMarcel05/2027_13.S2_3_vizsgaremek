using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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

        public WorkspaceController(IWorkspaceService workspaceService)
        {
            _workspaceService = workspaceService;
        }

        // 1. SAJÁT MUNKATERÜLETEK LISTÁZÁSA
        [HttpGet("my")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetMyWorkspaces([FromQuery] int page = 1, [FromQuery] int limit = 20)
        {
            if (page < 1 || limit < 1 || limit > 100)
                throw new ArgumentException("Érvénytelen lapozási paraméterek (page >= 1, 1 <= limit <= 100).");

            var userId = User.GetUserId();
            var workspaces = await _workspaceService.GetUserWorkspacesAsync(userId, page, limit);
            return Ok(workspaces);
        }

        // 2. EGYEDI MUNKATERÜLET ADATAI
        [HttpGet("{workspaceId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetWorkspaceData(Guid workspaceId)
        {
            var workspace = await _workspaceService.GetWorkspaceDataAsync(workspaceId)
                ?? throw new KeyNotFoundException("A megadott munkaterület nem található.");

            return Ok(workspace);
        }

        // 3. MUNKATERÜLET TAGJAINAK LISTÁZÁSA
        [HttpGet("{workspaceId:guid}/members")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetWorkspaceMembers(
            Guid workspaceId,
            [FromQuery] int page = 1,
            [FromQuery] int limit = 50,
            [FromQuery] Guid? role = null)
        {
            if (page < 1 || limit < 1 || limit > 100)
                throw new ArgumentException("Érvénytelen lapozási paraméterek (page >= 1, 1 <= limit <= 100).");

            var members = await _workspaceService.GetWorkspaceMembersAsync(workspaceId, page, limit, role);
            return Ok(members);
        }

        // 4. MUNKATERÜLET TÁRHELY-HASZNÁLATA
        [HttpGet("{workspaceId:guid}/storage")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetWorkspaceStorage(Guid workspaceId)
        {
            var storage = await _workspaceService.GetWorkspaceStorageAsync(workspaceId)
                ?? throw new KeyNotFoundException("A megadott munkaterület nem található.");

            return Ok(storage);
        }

        // 5. MUNKATERÜLET PROJEKTJEINEK LISTÁZÁSA
        [HttpGet("{workspaceId:guid}/projects")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetWorkspaceProjects(
            Guid workspaceId,
            [FromQuery] int page = 1,
            [FromQuery] int limit = 20,
            [FromQuery] sbyte? status = null)
        {
            if (page < 1 || limit < 1 || limit > 100)
                throw new ArgumentException("Érvénytelen lapozási paraméterek (page >= 1, 1 <= limit <= 100).");

            var projects = await _workspaceService.GetWorkspaceProjectsAsync(workspaceId, page, limit, status);
            return Ok(projects);
        }

        // 6. ÚJ MUNKATERÜLET LÉTREHOZÁSA
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateWorkspace([FromBody] CreateWorkspaceDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.workspaceName))
                throw new ArgumentException("A munkaterület neve nem lehet üres.");

            var userId = User.GetUserId();
            // Ha a Service-ben már létezik a név, dobjon ott: throw new InvalidOperationException("Már létezik ilyen nevű munkaterület.");
            var workspace = await _workspaceService.CreateWorkspaceAsync(userId, dto);
            return CreatedAtAction(nameof(GetWorkspaceData), new { workspaceId = workspace.workspaceId }, workspace);
        }

        // 7. ÚJ PROJEKT LÉTREHOZÁSA A MUNKATERÜLETEN BELÜL
        [HttpPost("{workspaceId:guid}/project")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateProject(Guid workspaceId, [FromBody] CreateProjectDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.name))
                throw new ArgumentException("A projekt neve nem lehet üres.");

            var project = await _workspaceService.CreateProjectAsync(workspaceId, dto);
            return StatusCode(StatusCodes.Status201Created, project);
        }

        // 8. MUNKATERÜLET-VÁLTÁS
        [HttpPost("{workspaceId:guid}/switch")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<WorkspaceTokenDto>> SwitchWorkspace(Guid workspaceId)
        {
            var userId = User.GetUserId();
            // A Service által dobott UnauthorizedAccessException-t a GlobalExceptionHandler automatikusan 403-ra alakítja
            var token = await _workspaceService.SwitchWorkspaceAsync(userId, workspaceId);
            return Ok(new WorkspaceTokenDto(token));
        }

        // 9. MUNKATERÜLET TÖRLÉSE
        [HttpDelete("{workspaceId:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteWorkspace(Guid workspaceId)
        {
            var result = await _workspaceService.DeleteWorkspaceAsync(workspaceId);
            if (!result)
                throw new KeyNotFoundException("A megadott munkaterület nem található.");

            return NoContent();
        }

        // 10. MUNKATERÜLET ADATAINAK MÓDOSÍTÁSA
        [HttpPut("{workspaceId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateWorkspace(Guid workspaceId, [FromBody] WorkspaceDataChangeDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.workspaceName))
                throw new ArgumentException("A munkaterület neve nem lehet üres.");

            var result = await _workspaceService.UpdateWorkspaceAsync(workspaceId, dto)
                ?? throw new KeyNotFoundException("A megadott munkaterület nem található.");

            return Ok(result);
        }

        // 11. TULAJDONOSVÁLTÁS
        [HttpPut("{workspaceId:guid}/owner")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ChangeWorkspaceOwner(Guid workspaceId, [FromBody] WorkspaceOwnerChangeDto dto)
        {
            if (dto.newOwnerId == Guid.Empty)
                throw new ArgumentException("Az új tulajdonos azonosítójának megadása kötelező.");

            var result = await _workspaceService.ChangeWorkspaceOwnerAsync(workspaceId, dto)
                ?? throw new KeyNotFoundException("A megadott munkaterület nem található.");

            return Ok(result);
        }

        // 12. TAG SZEREPKÖRÉNEK MÓDOSÍTÁSA
        [HttpPut("{workspaceId:guid}/members/{userId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateMemberRole(Guid workspaceId, Guid userId, [FromBody] WorkspaceMemberRoleChangeDto dto)
        {
            var result = await _workspaceService.UpdateMemberRoleAsync(workspaceId, userId, dto)
                ?? throw new KeyNotFoundException("A tag vagy munkaterület nem található.");

            return Ok(result);
        }

        // 13. TAG ELTÁVOLÍTÁSA A MUNKATERÜLETRŐL
        [HttpDelete("{workspaceId:guid}/members/{userId:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RemoveMember(Guid workspaceId, Guid userId)
        {
            var result = await _workspaceService.RemoveMemberAsync(workspaceId, userId);
            if (!result)
                throw new KeyNotFoundException("A tag vagy munkaterület nem található.");

            return NoContent();
        }

        // 14. FELADAT LEFOGLALÁSA
        [HttpPost("tasks/reserve")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> ReserveTask([FromBody] ReserveTaskDto dto)
        {
            var currentUserId = User.GetUserId();
            var secureDto = dto with { assigneeId = currentUserId };

            // A Service által dobott KeyNotFoundException (404), InvalidOperationException (409) 
            // és UnauthorizedAccessException (403) automatikusan a megfelelő JSON választ adja!
            var result = await _workspaceService.ReserveTaskAsync(secureDto);
            return Ok(result);
        }

        // 15. FELADAT LEFOGLALÁSÁNAK LEMONDÁSA
        [HttpPost("tasks/unreserve")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> UnReserveTask([FromBody] ReserveTaskDto dto)
        {
            var currentUserId = User.GetUserId();
            var secureDto = dto with { assigneeId = currentUserId };

            await _workspaceService.UnReserveTaskAsync(secureDto);
            return Ok(new { message = "A feladat lefoglalása sikeresen lemondva." });
        }
    }
}