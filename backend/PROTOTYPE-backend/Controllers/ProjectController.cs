using Microsoft.AspNetCore.Mvc;
using PROTOTYPE_backend.DTOs.Workspace;
using PROTOTYPE_backend.DTOs.Project;
using PROTOTYPE_backend.Services.Projects;
using System;
using System.Threading.Tasks;

namespace PROTOTYPE_backend.Controllers
{
    [ApiController]
    [Route("api/projects")]
    public class ProjectController : ControllerBase
    {
        private readonly IProjectService _projectService;

        public ProjectController(IProjectService projectService)
        {
            _projectService = projectService;
        }
        [HttpGet("{workspaceId}/projects")]
        public async Task<IActionResult> GetWorkspaceProjects(Guid workspaceId, [FromQuery] int page = 1, [FromQuery] int limit = 20, [FromQuery] sbyte? status = null)
        {
            var projects = await _projectService.GetWorkspaceByIdAsync(workspaceId);
            return Ok(projects);
        }

        [HttpPost("{projectId}/task")]
        public async Task<IActionResult> CreateTask(Guid projectId, [FromBody] CreateTaskRequestDto dto)
        {
            var task = await _projectService.CreateTaskAsync(projectId, dto.Title, dto.Description, dto.Priority, dto.DueDate, dto.AssigneeId);
            return Ok(task);
        }

        [HttpPut("task/{taskId}/status")]
        public async Task<IActionResult> UpdateTaskStatus(Guid taskId, [FromBody] ProjectDtos dto)
        {
            var success = await _projectService.UpdateTaskStatusAsync(taskId, dto.NewStatus);
            if (!success) return NotFound();
            return Ok();
        }
    }
}
