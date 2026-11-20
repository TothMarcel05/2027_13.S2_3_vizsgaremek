using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PROTOTYPE_backend.DTOs.Project;
using PROTOTYPE_backend.DTOs.Workspace;
using PROTOTYPE_backend.Services.Projects;
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

        public ProjectController(IProjectService projectService, ILogger<ProjectController> logger)
        {
            _projectService = projectService;
            _logger = logger;
        }

        // 1. MUNKATERÜLET PROJEKTJEINEK LISTÁZÁSA
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
            try
            {
                var projects = await _projectService.GetWorkspaceByIdAsync(workspaceId);
                return Ok(projects);
            }
            catch (Exception ex) when (ex.Message.Contains("Nem létezik"))
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Váratlan hiba történt a munkaterület projektjeinek lekérdezésekor. WorkspaceId: {WorkspaceId}", workspaceId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Szerveroldali hiba történt a projektek lekérdezésekor." });
            }
        }

        // 2. EGYEDI PROJEKT ADATAINAK LEKÉRDEZÉSE
        [HttpGet("{projectId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetProjectData(Guid projectId)
        {
            try
            {
                var project = await _projectService.GetProjectByIdAsync(projectId);

                if (project == null)
                {
                    return NotFound(new { message = "A megadott projekt nem található." });
                }

                return Ok(project);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Váratlan hiba történt a projekt lekérdezésekor. ProjectId: {ProjectId}", projectId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Szerveroldali hiba történt a projekt lekérdezésekor." });
            }
        }

        // 3. PROJEKT FELADATAINAK LISTÁZÁSA
        [HttpGet("{projectId:guid}/tasks")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetProjectTaskList(
            Guid projectId,
            [FromQuery] sbyte? status = null,
            [FromQuery] byte? priority = null)
        {
            try
            {
                var tasks = await _projectService.GetProjectTasksAsync(projectId, status, priority);
                return Ok(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Váratlan hiba történt a projekt feladatainak lekérdezésekor. ProjectId: {ProjectId}", projectId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Szerveroldali hiba történt a feladatok lekérdezésekor." });
            }
        }

        // 4. EGYEDI FELADAT LEKÉRDEZÉSE
        [HttpGet("tasks/{taskId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetProjectTask(Guid taskId)
        {
            try
            {
                var task = await _projectService.GetProjectTaskAsync(taskId);
                if (task == null)
                {
                    return NotFound(new { message = "A megadott feladat nem található." });
                }

                return Ok(task);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Váratlan hiba történt a feladat lekérdezésekor. TaskId: {TaskId}", taskId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Szerveroldali hiba történt a feladat lekérdezésekor." });
            }
        }

        // 5. ÚJ FELADAT LÉTREHOZÁSA
        [HttpPost("{projectId:guid}/task")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateTask(Guid projectId, [FromBody] CreateTaskRequestDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Title))
            {
                return BadRequest(new { message = "A feladat címe nem lehet üres." });
            }

            try
            {
                var task = await _projectService.CreateTaskAsync(projectId, dto.Title, dto.Description, dto.Priority, dto.DueDate, dto.AssigneeId);
                return CreatedAtAction(nameof(GetProjectTask), new { taskId = task.Id }, task);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Váratlan hiba történt új feladat létrehozásakor. ProjectId: {ProjectId}", projectId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Szerveroldali hiba történt a feladat létrehozásakor." });
            }
        }

        // 6. FELADAT STÁTUSZÁNAK MÓDOSÍTÁSA
        [HttpPut("task/{taskId:guid}/status")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateTaskStatus(Guid taskId, [FromBody] ProjectDtos dto)
        {
            try
            {
                var success = await _projectService.UpdateTaskStatusAsync(taskId, dto.NewStatus);
                if (!success)
                {
                    return NotFound(new { message = "A megadott feladat nem található." });
                }

                return Ok(new { message = "A feladat státusza sikeresen frissítve." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Váratlan hiba történt a feladat státuszának módosításakor. TaskId: {TaskId}", taskId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Szerveroldali hiba történt a státusz módosításakor." });
            }
        }

        // 7. PROJEKT ADATAINAK MÓDOSÍTÁSA
        [HttpPut("{projectId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateProject(Guid projectId, [FromBody] UpdateProjectDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                return BadRequest(new { message = "A projekt neve nem lehet üres." });
            }

            try
            {
                var updatedProject = await _projectService.UpdateProjectAsync(projectId, dto);
                if (updatedProject == null)
                {
                    return NotFound(new { message = "A megadott projekt nem található." });
                }

                return Ok(updatedProject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Váratlan hiba történt a projekt módosításakor. ProjectId: {ProjectId}", projectId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Szerveroldali hiba történt a projekt módosításakor." });
            }
        }

        // 8. FELADAT MÓDOSÍTÁSA (kezdő "/" eltávolítva -> api/projects/tasks/{taskId})
        [HttpPut("tasks/{taskId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateTask(Guid taskId, [FromBody] UpdateTaskDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Title))
            {
                return BadRequest(new { message = "A feladat címe nem lehet üres." });
            }

            try
            {
                var updatedTask = await _projectService.UpdateTaskAsync(taskId, dto);
                if (updatedTask == null)
                {
                    return NotFound(new { message = "A megadott feladat nem található." });
                }

                return Ok(updatedTask);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Váratlan hiba történt a feladat módosításakor. TaskId: {TaskId}", taskId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Szerveroldali hiba történt a feladat módosításakor." });
            }
        }

        // 9. PROJEKT TÖRLÉSE
        [HttpDelete("{projectId:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteProject(Guid projectId)
        {
            try
            {
                var success = await _projectService.DeleteProjectAsync(projectId);
                if (!success)
                {
                    return NotFound(new { message = "A megadott projekt nem található." });
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Váratlan hiba történt a projekt törlésekor. ProjectId: {ProjectId}", projectId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Szerveroldali hiba történt a projekt törlésekor." });
            }
        }

        // 10. FELADAT TÖRLÉSE (kezdő "/" eltávolítva -> api/projects/tasks/{taskId})
        [HttpDelete("tasks/{taskId:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteTask(Guid taskId)
        {
            try
            {
                var success = await _projectService.DeleteTaskAsync(taskId);
                if (!success)
                {
                    return NotFound(new { message = "A megadott feladat nem található." });
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Váratlan hiba történt a feladat törlésekor. TaskId: {TaskId}", taskId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Szerveroldali hiba történt a feladat törlésekor." });
            }
        }
    }
}