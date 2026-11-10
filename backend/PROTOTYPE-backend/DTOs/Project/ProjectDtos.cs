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

    #endregion

    #region Response DTOs

    public record ProjectResponseDto(
        Guid projectId,
        string projectName,
        Guid workspaceId
        );

    #endregion
}
