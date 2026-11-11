using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using PROTOTYPE_backend.DTOs.Storage;

namespace PROTOTYPE_backend.Services.Storage
{
    public interface IStorageService
    {

        Task<NodeDataChangeResponseDto> ChangeNodeData(GetNodesRequestDto dto);
        Task<StorageNodeResponseDto> UploadFileAsync(UploadFileDto dto);
        Task<StorageNodeResponseDto> CreateFolderAsync(Guid workspaceId, Guid? projectId, Guid? parentId, string folderName, Guid userId);
        Task<(Stream FileStream, string ContentType, string FileName)?> GetFileStreamAsync(Guid nodeId);

        Task<IEnumerable<StorageNodeResponseDto>> GetNodesByWorkspaceAsync(Guid workspaceId, string? tagFilter);

        Task<bool> ShareNodeWithRoleAsync(Guid nodeId, Guid roleId);

        Task<bool> SoftDeleteNodeAsync(Guid nodeId, Guid userId);
        Task<bool> RestoreNodeAsync(Guid nodeId);
    }
}