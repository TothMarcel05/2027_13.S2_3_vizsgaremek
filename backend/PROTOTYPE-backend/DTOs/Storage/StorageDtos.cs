using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace PROTOTYPE_backend.DTOs.Storage
{
    public record UploadFileDto(
        [Required] Guid WorkspaceId,
        Guid? ProjectId,
        Guid? ParentId,
        [Required] IFormFile File,
        List<string>? Labels,
        [Required] Guid UserId
    );

    public record ShareNodeDto(
        [Required] Guid NodeId,
        [Required] Guid RoleId
    );

    public record StorageNodeResponseDto(
        Guid Id,
        string Name,
        sbyte Type,
        string? MimeType,
        ulong SizeBytes,
        DateTime CreatedAt,
        List<string> Labels
    );
}