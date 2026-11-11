using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace PROTOTYPE_backend.DTOs.Storage
{

    #region Request DTOs


    public record UploadFileDto(
        [Required] Guid WorkspaceId,
        Guid? ProjectId,
        Guid? ParentId,
        [Required] IFormFile File,
        List<string>? Labels,
        [Required] Guid UserId
    );

    public record CreateFolderDto(
        [Required] Guid WorkspaceId,
        Guid? ProjectId,
        Guid? ParentId,
        [Required] string FolderName
    );

    public record ShareNodeDto(
        [Required] Guid NodeId,
        [Required] Guid RoleId
    );

    public record StorageNodeResponseDto(
        Guid Id,
        Guid WorkspaceId,
        string Name,
        sbyte Type,
        string? MimeType,
        ulong? SizeBytes,
        DateTime? CreatedAt,
        List<string> Labels
    );

    public record GetNodesRequestDto(
    [Required] Guid WorkspaceId,
    [Required] Guid? NodeId
    );
    #endregion

    #region Response DTOs
    public record NodeDataChangeResponseDto(
        Guid Id,
        Guid WorkspaceId,
        string Name,
        string? StoragePath

    );

    #endregion 
}