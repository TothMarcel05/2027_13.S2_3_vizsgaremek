using System.ComponentModel.DataAnnotations;

namespace PROTOTYPE_backend.DTOs.Calendar
{
    // Létrehozási kérelem
    public record DateRequestDto(
        Guid? ProjectId,

        [Required]
        [StringLength(100, MinimumLength = 3)]
        string Title,

        string? Description,

        [StringLength(200)]
        string? Location,

        [Required]
        DateTime StartTime,

        [Required]
        DateTime EndTime,

        bool IsAllDay = false
    );

    // Frissítési kérelem
    public record DateUpdateDto(
        Guid? ProjectId,

        [Required]
        [StringLength(100, MinimumLength = 3)]
        string Title,

        string? Description,

        [StringLength(200)]
        string? Location,

        [Required]
        DateTime StartTime,

        [Required]
        DateTime EndTime,

        bool IsAllDay
    );

    // Válasz DTO
    public record DateDto(
        Guid Id,
        Guid? ProjectId,
        string Title,
        string? Description,
        string? Location,
        DateTime StartTime,
        DateTime EndTime,
        bool IsAllDay,
        Guid? CreatedBy
    );
}