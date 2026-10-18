using System;
using System.ComponentModel.DataAnnotations;

namespace PROTOTYPE_backend.DTOs.Workspace
{
    public record CreateWorkspaceDto(
        [Required] string UserId,
        [Required] string Name
    );

    public record CreateProjectDto(
        [Required] string Name,
        string? Description,
        DateTime? Deadline,
        string? OwnerId
    );

    public record CreateTaskDto(
        [Required] string Title,
        string? Description,
        byte Priority,
        DateTime? DueDate,
        string? AssigneeId,
        string? ReporterId
    );

    public record UpdateTaskStatusDto(
        [Required] byte NewStatus 
    );
}