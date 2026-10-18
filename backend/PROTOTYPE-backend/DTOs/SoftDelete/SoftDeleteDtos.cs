using System;
using System.ComponentModel.DataAnnotations;

namespace PROTOTYPE_backend.DTOs.SoftDelete
{
    public record SoftDeleteRequestDto(
        [Required] string EntityName, 
        [Required] string EntityId,
        [Required] string UserId
    );

    public record RestoreRequestDto(
        [Required] string EntityName,
        [Required] string EntityId
    );

    public record DeletedItemDto(
        string EntityName,
        string EntityId,
        DateTime DeletedAt,
        DateTime RetentionExpiresAt,
        bool CanRestore
    );
}