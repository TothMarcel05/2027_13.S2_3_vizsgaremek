using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PROTOTYPE_backend.DTOs.Workspace;
using PROTOTYPE_backend.Services.Workspace;
using System.Security.Claims;

namespace PROTOTYPE_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WorkspaceController : ControllerBase
{
    private readonly IWorkspaceService _workspaceService;

    public WorkspaceController(IWorkspaceService workspaceService)
    {
        _workspaceService = workspaceService;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> GetWorkspaces()
    {
        var result = await _workspaceService.GetUserWorkspacesAsync(GetUserId());
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateWorkspace([FromBody] CreateWorkspaceDto dto)
    {
        var result = await _workspaceService.CreateWorkspaceAsync(dto, GetUserId());
        return Ok(result);
    }

    [HttpPost("project")]
    public async Task<IActionResult> CreateProject([FromBody] CreateProjectDto dto)
    {
        var result = await _workspaceService.CreateProjectAsync(dto, GetUserId());
        return Ok(result);
    }

    [HttpPost("task")]
    public async Task<IActionResult> CreateTask([FromBody] CreateTaskDto dto)
    {
        var result = await _workspaceService.CreateTaskAsync(dto, GetUserId());
        return Ok(result);
    }

    [HttpPatch("task/status")]
    public async Task<IActionResult> UpdateTaskStatus([FromBody] UpdateTaskStatusDto dto)
    {
        var success = await _workspaceService.UpdateTaskStatusAsync(dto, GetUserId());
        if (!success) return BadRequest("Érvénytelen feladat vagy állapot.");
        return Ok(new { message = "Feladat állapota sikeresen frissítve." });
    }
}