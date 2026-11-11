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

        [HttpGet("{projectId}")]
        public async Task<IActionResult> GetProjectData(Guid projectId)
        {
            var project = await _projectService.GetProjectByIdAsync(projectId);

            if (project == null)
            {
                return NotFound(new { message = "A megadott projekt nem található." });
            }

            return Ok(project);
        }


        [HttpGet("{projectId}/tasks")]
        public async Task<IActionResult> GetProjectTaskList(Guid projectId, [FromQuery] sbyte? status = null, [FromQuery] byte? priority = null)
        {
            var tasks = await _projectService.GetProjectTasksAsync(projectId, status, priority);
            return Ok(tasks);
        }

        [HttpGet("tasks/{taskId}")]
        public async Task<IActionResult> GetProjectTask(Guid taskId)
        {
            var tasks = await _projectService.GetProjectTaskAsync(taskId);
            return Ok(tasks);
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

        [HttpPut("{projectId}")]
        public async Task<IActionResult> UpdateProject(Guid projectId, [FromBody] UpdateProjectDto dto)
        {
            var updatedProject = await _projectService.UpdateProjectAsync(projectId, dto);
            if (updatedProject == null)
                return NotFound(new { message = "A megadott projekt nem található." });

            return Ok(updatedProject);
        }

        [HttpPut("/tasks/{taskId}")]
        public async Task<IActionResult> UpdateTask(Guid taskId, [FromBody] UpdateTaskDto dto)
        {
            var updatedTask = await _projectService.UpdateTaskAsync(taskId, dto);
            if (updatedTask == null)
                return NotFound(new { message = "A megadott feladat nem található." });

            return Ok(updatedTask);
        }

        [HttpDelete("{projectId}")]
        public async Task<IActionResult> DeleteProject(Guid projectId)
        {
            var success = await _projectService.DeleteProjectAsync(projectId);
            if (!success)
                return NotFound(new { message = "A megadott projekt nem található." });

            return Ok(new { message = "Projekt sikeresen törölve." });
        }

        [HttpDelete("/tasks/{taskId}")]
        public async Task<IActionResult> DeleteTask(Guid taskId)
        {
            var success = await _projectService.DeleteTaskAsync(taskId);
            if (!success)
                return NotFound(new { message = "A megadott feladat nem található." });

            return Ok(new { message = "Feladat sikeresen törölve." });
        }
    }
}
