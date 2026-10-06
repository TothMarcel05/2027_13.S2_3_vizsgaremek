using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

namespace PROTOTYPE_backend.DTOs.Calendar
{
    public record DateRequestDto(
            [Required]
            Guid ProjectId,
            [Required]
            DateTime timeStamp,
            [Required]
            [StringLength(25, MinimumLength = 3)]
            string Title,
            [Required]
            [StringLength(120, MinimumLength = 10)]
            string Description
        );
}
