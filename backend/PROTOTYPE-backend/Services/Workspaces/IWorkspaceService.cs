using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PROTOTYPE_backend.DTOs.Workspace;

namespace PROTOTYPE_backend.Services.Workspaces
{
    public interface IWorkspaceService
    {
        Task<WorkspaceDataResponseDto> CreateWorkspaceAsync(Guid userId, CreateWorkspaceDto dto);

        Task<List<WorkspaceResponseDto>> GetUserWorkspacesAsync(Guid userId);

        Task<WorkspaceDataResponseDto?> GetWorkspaceDataAsync(Guid workspaceId);

        Task<List<WorkspaceProjectResponseDto>> GetWorkspaceProjectsAsync(Guid workspaceId, int page = 1, int limit = 20, sbyte? status = null);

        Task<List<WorkspaceMemberResponseDto>> GetWorkspaceMembersAsync(Guid workspaceId, int page = 1, int limit = 50, Guid? role = null);

        Task<WorkspaceProjectResponseDto> CreateProjectAsync(Guid workspaceId, CreateProjectDto dto);

        Task<string> SwitchWorkspaceAsync(Guid userId, Guid workspaceId);

        Task<bool> DeleteWorkspaceAsync (Guid workspaceId);
    }
}