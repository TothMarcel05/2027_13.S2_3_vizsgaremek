using System.ComponentModel.DataAnnotations;

namespace PROTOTYPE_backend.DTOs.Project
{
    public record CreateTaskRequestDto(
         [Required] string Title,
         string? Description,
         byte Priority,
         DateTime? DueDate,
         Guid? AssigneeId = null
     );
}
