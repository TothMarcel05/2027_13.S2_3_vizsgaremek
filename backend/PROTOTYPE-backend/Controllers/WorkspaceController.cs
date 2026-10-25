using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PROTOTYPE_backend.DTOs.Auth;
using PROTOTYPE_backend.DTOs.Workspace;
using PROTOTYPE_backend.Models;
using PROTOTYPE_backend.Services.Workspaces;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace PROTOTYPE_backend.Controllers
{
    [ApiController]
    [Route("api/workspace")]
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
            var workspace = await _workspaceService.CreateWorkspaceAsync(dto.UserId, dto.WorkspaceName);
            return Ok(workspace);
        }


        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserWorkspaces(Guid userId)
        {
            var workspaces = await _workspaceService.GetUserWorkspacesAsync(userId);
            return Ok(workspaces);
        }


        [HttpGet("/workspaces/{workspaceId}")]
        public async Task<IActionResult> GetWorkspaceData(Guid WorkspaceId)
        {
            var workspace = await _workspaceService.GetWorkspaceDataAsync(WorkspaceId);

            if (workspace == null)
            {
                return NotFound(new { message = "A megadott munkaterület nem található." });
            }

            return Ok(workspace);
        }

        [HttpGet("/workspaces/{workspaceId}/projects")]
        public async Task<IActionResult> GetWorkspaceProjects(Guid workspaceId, [FromQuery] int page = 1, [FromQuery] int limit = 20, [FromQuery] sbyte? status = null)
        {
            var projects = await _workspaceService.GetWorkspaceProjectAsync(workspaceId, page, limit, status);
            return Ok(projects);
        }

        [HttpGet("/workspaces/{workspaceId}/members")]
        public async Task<IActionResult> GetWorkspaceMembers(Guid workspaceId, [FromQuery] int page = 1, [FromQuery] int limit = 50, [FromQuery] Guid? Role = null)
        {
            var members = await _workspaceService.GetWorkspaceMembersAsync(workspaceId, page, limit, Role);
            return Ok(members);
        }

        [HttpPost("{workspaceId}/project")]
        public async Task<IActionResult> CreateProject(Guid workspaceId, [FromBody] CreateProjectDto dto)
        {
            var project = await _workspaceService.CreateProjectAsync(workspaceId, dto.Name, dto.Description, dto.Deadline, dto.OwnerId);
            return Ok(project);
        }

        [HttpPost("{workspaceId:guid}/switch")]
        public async Task<ActionResult<WorkspaceTokenDto>> SwitchWorkspace(Guid workspaceId)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            var token = await _workspaceService.SwitchWorkspaceAsync(userId, workspaceId);

            return Ok(new WorkspaceTokenDto(token));
        }
    }
}