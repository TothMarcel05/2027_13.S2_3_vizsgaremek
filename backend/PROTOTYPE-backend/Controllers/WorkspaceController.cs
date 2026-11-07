using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PROTOTYPE_backend.DTOs.Auth;
using PROTOTYPE_backend.DTOs.Workspace;
using PROTOTYPE_backend.Services.Workspaces;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace PROTOTYPE_backend.Controllers
{
    [ApiController]
    [Route("api/workspaces")] // A táblázat a /workspaces végpontot használja
    //[Authorize]
    public class WorkspaceController : ControllerBase
    {
        private readonly IWorkspaceService _workspaceService;

        public WorkspaceController(IWorkspaceService workspaceService)
        {
            _workspaceService = workspaceService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateWorkspace([FromBody] CreateWorkspaceDto dto)
        {
            var workspace = await _workspaceService.CreateWorkspaceAsync(dto.userId, dto);
            return Ok(workspace);
        }

        // Ha a user saját listáját kéred, egy query paraméter tisztább lenne, 
        // de meghagytam a korábbi user/{userId} útvonalat is kompatibilitás miatt.
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserWorkspaces(Guid userId)
        {
            var workspaces = await _workspaceService.GetUserWorkspacesAsync(userId);
            return Ok(workspaces);
        }

        [HttpGet("{workspaceId}")]
        public async Task<IActionResult> GetWorkspaceData(Guid workspaceId)
        {
            var workspace = await _workspaceService.GetWorkspaceDataAsync(workspaceId);
            if (workspace == null) return NotFound(new { message = "A megadott munkaterület nem található." });
            return Ok(workspace);
        }

        [HttpGet("{workspaceId}/projects")]
        public async Task<IActionResult> GetWorkspaceProjects(Guid workspaceId, [FromQuery] int page = 1, [FromQuery] int limit = 20, [FromQuery] sbyte? status = null)
        {
            var projects = await _workspaceService.GetWorkspaceProjectsAsync(workspaceId, page, limit, status);
            return Ok(projects);
        }

        [HttpGet("{workspaceId}/members")]
        public async Task<IActionResult> GetWorkspaceMembers(Guid workspaceId, [FromQuery] int page = 1, [FromQuery] int limit = 50, [FromQuery] Guid? role = null)
        {
            var members = await _workspaceService.GetWorkspaceMembersAsync(workspaceId, page, limit, role);
            return Ok(members);
        }

        [HttpPost("{workspaceId}/project")]
        public async Task<IActionResult> CreateProject(Guid workspaceId, [FromBody] CreateProjectDto dto)
        {
            var project = await _workspaceService.CreateProjectAsync(workspaceId, dto);
            return Ok(project);
        }

        [HttpPost("{workspaceId:guid}/switch")]
        public async Task<ActionResult<WorkspaceTokenDto>> SwitchWorkspace(Guid workspaceId)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaim, out var userId)) return Unauthorized();

            var token = await _workspaceService.SwitchWorkspaceAsync(userId, workspaceId);
            return Ok(new WorkspaceTokenDto(token));
        }

        [HttpDelete("{workspaceId}")]
        public async Task<IActionResult> DeleteWorkspace(Guid workspaceId)
        {
            var result = await _workspaceService.DeleteWorkspaceAsync(workspaceId);
            if (!result) return NotFound(new { message = "A megadott munkaterület nem található." });
            return Ok(new { message = "A munkaterület sikeresen törölve lett." });
        }

        [HttpGet("{workspaceId}/storage")]
        public async Task<IActionResult> GetWorkspaceStorage(Guid workspaceId)
        {
            var storage = await _workspaceService.GetWorkspaceStorageAsync(workspaceId);
            if (storage == null) return NotFound(new { message = "A megadott munkaterület nem található." });
            return Ok(storage);
        }

        [HttpPut("{workspaceId}")]
        public async Task<IActionResult> UpdateWorkspace(Guid workspaceId, [FromBody] WorkspaceDataChangeDto dto)
        {
            var result = await _workspaceService.UpdateWorkspaceAsync(workspaceId, dto);
            if (result == null) return NotFound(new { message = "A megadott munkaterület nem található." });
            return Ok(result);
        }

        [HttpPut("{workspaceId}/owner")]
        public async Task<IActionResult> ChangeWorkspaceOwner(Guid workspaceId, [FromBody] WorkspaceOwnerChangeDto dto)
        {
            var result = await _workspaceService.ChangeWorkspaceOwnerAsync(workspaceId, dto);
            if (result == null) return NotFound(new { message = "A megadott munkaterület nem található." });
            return Ok(result);
        }

        [HttpPut("{workspaceId}/members/{userId}")]
        public async Task<IActionResult> UpdateMemberRole(Guid workspaceId, Guid userId, [FromBody] WorkspaceMemberRoleChangeDto dto)
        {
            var result = await _workspaceService.UpdateMemberRoleAsync(workspaceId, userId, dto);
            if (result == null) return NotFound(new { message = "A tag vagy munkaterület nem található." });
            return Ok(result);
        }

        [HttpDelete("{workspaceId}/members/{userId}")]
        public async Task<IActionResult> RemoveMember(Guid workspaceId, Guid userId)
        {
            var result = await _workspaceService.RemoveMemberAsync(workspaceId, userId);
            if (!result) return NotFound(new { message = "A tag vagy munkaterület nem található." });
            return Ok(new { message = "A tag sikeresen el lett távolítva." });
        }
    }
}