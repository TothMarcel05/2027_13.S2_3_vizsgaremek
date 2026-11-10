using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PROTOTYPE_backend.DTOs.Workspace;

namespace PROTOTYPE_backend.Services.Workspaces
{
    public interface IWorkspaceService
    {
        Task<List<WorkspaceResponseDto>> GetUserWorkspacesAsync(Guid userId, int page = 1, int limit = 20);
        Task<WorkspaceDataResponseDto> CreateWorkspaceAsync(Guid userId, CreateWorkspaceDto dto);
        Task<WorkspaceDataResponseDto?> GetWorkspaceDataAsync(Guid workspaceId);
        Task<List<WorkspaceProjectResponseDto>> GetWorkspaceProjectsAsync(Guid workspaceId, int page = 1, int limit = 20, sbyte? status = null);
        Task<List<WorkspaceMemberResponseDto>> GetWorkspaceMembersAsync(Guid workspaceId, int page = 1, int limit = 50, Guid? role = null);
        Task<WorkspaceProjectResponseDto> CreateProjectAsync(Guid workspaceId, CreateProjectDto dto);
        Task<string> SwitchWorkspaceAsync(Guid userId, Guid workspaceId);
        Task<bool> DeleteWorkspaceAsync(Guid workspaceId);
        Task<WorkspaceStorageUsageResponseDto?> GetWorkspaceStorageAsync(Guid workspaceId);
        Task<WorkspaceDataChangeResponseDto?> UpdateWorkspaceAsync(Guid workspaceId, WorkspaceDataChangeDto dto);
        Task<WorkspaceOwnerChangeResponseDto?> ChangeWorkspaceOwnerAsync(Guid workspaceId, WorkspaceOwnerChangeDto dto);
        Task<WorkspaceMemberRoleChangeResponseDto?> UpdateMemberRoleAsync(Guid workspaceId, Guid userId, WorkspaceMemberRoleChangeDto dto);
        Task<bool> RemoveMemberAsync(Guid workspaceId, Guid userId);

    }
}