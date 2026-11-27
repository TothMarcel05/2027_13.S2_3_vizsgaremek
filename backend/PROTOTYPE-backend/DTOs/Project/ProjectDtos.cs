using System.ComponentModel.DataAnnotations;

namespace PROTOTYPE_backend.DTOs.Project
{
    #region Request DTOs

    public record ProjectDtos(
        [Required] byte NewStatus
    );
    public record CreateTaskRequestDto(
     [Required] string Title,
     string? Description,
     byte Priority,
     DateTime? DueDate,
     Guid? AssigneeId = null);

    public record UpdateProjectDto(
        [Required] string Name,
        string? Description,
        sbyte Status,
        DateOnly? Deadline
    );

    public record UpdateTaskDto(
        [Required] string Title,
        string? Description,
        sbyte Priority,
        sbyte Status,
        DateOnly? DueDate,
        Guid? AssigneeUserId
    );

    #endregion

    #region Response DTOs

    public record ProjectResponseDto(
        Guid projectId,
        string projectName,
        Guid workspaceId
        );


    public record ProjectDataDto(
        string projectName,
        string? description,
        sbyte status,
        DateOnly? deadline,
        Guid? ownerId,
        DateTime? createdAt,
        DateTime? updatedAt
    );

    public record ProjectTaskListDto(
        Guid id,
        Guid projectId,
        string title,
        string? description,
        sbyte priority,
        sbyte status,
        DateOnly? dueDate,
        Guid? assigneeUserId,
        DateTime? createdAt
    );

    public record ProjectTaskResponseDto(
        Guid id,
        Guid projectId,
        string title,
        string? description,
        sbyte priority,
        sbyte status,
        DateOnly? dueDate,
        Guid? assigneeUserId,
        DateTime? createdAt
    );



    #endregion
}
