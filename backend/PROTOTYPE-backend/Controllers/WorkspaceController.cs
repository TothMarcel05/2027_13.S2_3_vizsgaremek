using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PROTOTYPE_backend.DTOs.Workspace;
using PROTOTYPE_backend.Services.Workspace;

namespace PROTOTYPE_backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WorkspaceController : ControllerBase
    {
        private readonly IWorkspaceService _workspaceService;

        public WorkspaceController(IWorkspaceService workspaceService)
        {
            _workspaceService = workspaceService;
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserWorkspaces(string userId)
        {
            var workspaces = await _workspaceService.GetUserWorkspacesAsync(userId);
            return Ok(workspaces);
        }

        [HttpPost]
        public async Task<IActionResult> CreateWorkspace([FromBody] CreateWorkspaceDto dto)
        {
            var workspace = await _workspaceService.CreateWorkspaceAsync(dto.UserId, dto.Name);
            return Ok(workspace);
        }

        [HttpPost("{workspaceId}/project")]
        public async Task<IActionResult> CreateProject(Guid workspaceId, [FromBody] CreateProjectDto dto)
        {
            var project = await _workspaceService.CreateProjectAsync(workspaceId, dto.Name, dto.Description, dto.Deadline, dto.OwnerId);
            return Ok(project);
        }

        [HttpPost("project/{projectId}/task")]
        public async Task<IActionResult> CreateTask(Guid projectId, [FromBody] CreateTaskDto dto)
        {
            var task = await _workspaceService.CreateTaskAsync(projectId, dto.Title, dto.Description, dto.Priority, dto.DueDate, dto.AssigneeId, dto.ReporterId);
            return Ok(task);
        }

        [HttpPut("task/{taskId}/status")]
        public async Task<IActionResult> UpdateTaskStatus(Guid taskId, [FromBody] UpdateTaskStatusDto dto)
        {
            var success = await _workspaceService.UpdateTaskStatusAsync(taskId, dto.NewStatus);
            if (!success) return NotFound();
            return Ok();
        }
    }
}