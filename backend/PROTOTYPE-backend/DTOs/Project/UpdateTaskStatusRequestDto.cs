using System.ComponentModel.DataAnnotations;

namespace PROTOTYPE_backend.DTOs.Project
{
    public record UpdateTaskStatusRequestDto(
        [Required] byte NewStatus
    );
}
