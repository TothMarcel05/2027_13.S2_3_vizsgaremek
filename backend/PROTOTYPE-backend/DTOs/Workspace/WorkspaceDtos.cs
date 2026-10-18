using System;
using System.ComponentModel.DataAnnotations;

namespace PROTOTYPE_backend.DTOs.Workspace
{
    public record CreateWorkspaceDto(
        [Required] Guid UserId,
        [Required] string Name
    );

    public record CreateProjectDto(
        [Required] string Name,
        string? Description,
        DateTime? Deadline,
        Guid? OwnerId
    );
}