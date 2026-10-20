using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using PROTOTYPE_backend.DTOs.Storage;
using PROTOTYPE_backend.Models;

namespace PROTOTYPE_backend.Services.Storage
{
    public interface IStorageService
    {
        Task<StorageNodeResponseDto> UploadFileAsync(UploadFileDto dto);
        Task<IEnumerable<StorageNodeResponseDto>> GetNodesByWorkspaceAsync(Guid workspaceId, string? tagFilter);
        Task<bool> ShareNodeWithRoleAsync(Guid nodeId, Guid roleId);
        Task<bool> SoftDeleteNodeAsync(Guid nodeId, Guid userId);
        Task<bool> RestoreNodeAsync(Guid nodeId);
    }
}