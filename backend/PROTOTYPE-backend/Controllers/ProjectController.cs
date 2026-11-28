using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PROTOTYPE_backend.DTOs.Project;
using PROTOTYPE_backend.DTOs.Workspace;
using PROTOTYPE_backend.Extensions;
using PROTOTYPE_backend.Services.Projects;
using PROTOTYPE_backend.Services.Workspaces;
using System;
using System.Threading.Tasks;

namespace PROTOTYPE_backend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/projects")]
    public class ProjectController : ControllerBase
    {
        private readonly IProjectService _projectService;
        private readonly ILogger<ProjectController> _logger;

        private readonly IWorkspaceService _workspaceService;

        public ProjectController(IProjectService projectService, ILogger<ProjectController> logger, IWorkspaceService workspaceService)
        {
            _workspaceService = workspaceService;
            _projectService = projectService;
            _logger = logger;
        }

        // Projektet és feladatot a munkaterület tulajdonosa és tagjai, a projekt tagjai és a SystemAdmin olvashatnak
        private Task EnsureCanReadProjectAsync(Guid projectId) =>
            User.IsSystemAdmin()
                ? Task.CompletedTask
                : _projectService.EnsureProjectAccessAsync(projectId, User.GetUserId());

        // 1. MUNKATERÜLET PROJEKTJEINEK LISTÁZÁSA ([Authorize])
        [HttpGet("{workspaceId:guid}/projects")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetWorkspaceProjects(
            Guid workspaceId,
            [FromQuery] int page = 1,
            [FromQuery] int limit = 20,
            [FromQuery] sbyte? status = null)
        {
            if (!User.IsSystemAdmin())
                await _workspaceService.EnsureWorkspaceAccessAsync(workspaceId, User.GetUserId());

            var projects = await _projectService.GetWorkspaceByIdAsync(workspaceId);
            return Ok(projects);
            
        }

        // 2. EGYEDI PROJEKT ADATAINAK LEKÉRDEZÉSE ([Authorize])
        [HttpGet("{projectId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetProjectData(Guid projectId)
        {
            await EnsureCanReadProjectAsync(projectId);

            var project = await _projectService.GetProjectByIdAsync(projectId);

            if (project == null)
            {
                return NotFound(new { error = new { message = "A megadott projekt nem található.", code = "NEM_TALALHATO" } });
            }

            return Ok(project);
            
        }

        // 3. PROJEKT FELADATAINAK LISTÁZÁSA ([Authorize])
        [HttpGet("{projectId:guid}/tasks")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetProjectTaskList(
            Guid projectId,
            [FromQuery] sbyte? status = null,
            [FromQuery] byte? priority = null)
        {
            await EnsureCanReadProjectAsync(projectId);

            var tasks = await _projectService.GetProjectTasksAsync(projectId, status, priority);
            return Ok(tasks);
            
        }

        // 4. EGYEDI FELADAT LEKÉRDEZÉSE ([Authorize])
        [HttpGet("tasks/{taskId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetProjectTask(Guid taskId)
        {
            var task = await _projectService.GetProjectTaskAsync(taskId);
            if (task == null)
            {
                return NotFound(new { error = new { message = "A megadott feladat nem található.", code = "NEM_TALALHATO" } });
            }

            await EnsureCanReadProjectAsync(task.projectId);

            return Ok(task);
            
        }

        // 5. ÚJ FELADAT LÉTREHOZÁSA
        [HttpPost("{projectId:guid}/task")]
        [Authorize(Policy = "task.create")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateTask(Guid projectId, [FromBody] CreateTaskRequestDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Title))
            {
                return BadRequest(new { error = new { message = "A feladat címe nem lehet üres.", code = "ERVENYTELEN_KERES" } });
            }

            var task = await _projectService.CreateTaskAsync(projectId, dto.Title, dto.Description, dto.Priority, dto.DueDate, dto.AssigneeId);
            return CreatedAtAction(nameof(GetProjectTask), new { taskId = task.Id }, task);
            
        }

        // 6. FELADAT STÁTUSZÁNAK MÓDOSÍTÁSA
        [HttpPut("task/{taskId:guid}/status")]
        [Authorize(Policy = "task.edit")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateTaskStatus(Guid taskId, [FromBody] ProjectDtos dto)
        {
            var success = await _projectService.UpdateTaskStatusAsync(taskId, dto.NewStatus);
            if (!success)
            {
                return NotFound(new { error = new { message = "A megadott feladat nem található.", code = "NEM_TALALHATO" } });
            }

            return Ok(new { message = "A feladat státusza sikeresen frissítve." });
            
        }

        // 7. PROJEKT ADATAINAK MÓDOSÍTÁSA
        [HttpPut("{projectId:guid}")]
        [Authorize(Policy = "project.edit")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateProject(Guid projectId, [FromBody] UpdateProjectDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                return BadRequest(new { error = new { message = "A projekt neve nem lehet üres.", code = "ERVENYTELEN_KERES" } });
            }

            var updatedProject = await _projectService.UpdateProjectAsync(projectId, dto);
            if (updatedProject == null)
            {
                return NotFound(new { error = new { message = "A megadott projekt nem található.", code = "NEM_TALALHATO" } });
            }

            return Ok(updatedProject);
            
        }

        // 8. FELADAT MÓDOSÍTÁSA
        [HttpPut("tasks/{taskId:guid}")]
        [Authorize(Policy = "task.edit")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateTask(Guid taskId, [FromBody] UpdateTaskDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Title))
            {
                return BadRequest(new { error = new { message = "A feladat címe nem lehet üres.", code = "ERVENYTELEN_KERES" } });
            }

            var updatedTask = await _projectService.UpdateTaskAsync(taskId, dto);
            if (updatedTask == null)
            {
                return NotFound(new { error = new { message = "A megadott feladat nem található.", code = "NEM_TALALHATO" } });
            }

            return Ok(updatedTask);
            
        }

        // 9. PROJEKT TÖRLÉSE
        [HttpDelete("{projectId:guid}")]
        [Authorize(Policy = "project.delete")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteProject(Guid projectId)
        {
            var success = await _projectService.DeleteProjectAsync(projectId);
            if (!success)
            {
                return NotFound(new { error = new { message = "A megadott projekt nem található.", code = "NEM_TALALHATO" } });
            }

            return NoContent();
            
        }

        // 10. FELADAT TÖRLÉSE
        [HttpDelete("tasks/{taskId:guid}")]
        [Authorize(Policy = "task.delete")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteTask(Guid taskId)
        {
            var success = await _projectService.DeleteTaskAsync(taskId);
            if (!success)
            {
                return NotFound(new { error = new { message = "A megadott feladat nem található.", code = "NEM_TALALHATO" } });
            }

            return NoContent();
            
        }
    }
}