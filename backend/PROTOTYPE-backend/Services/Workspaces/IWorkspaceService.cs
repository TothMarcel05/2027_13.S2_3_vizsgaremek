using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PROTOTYPE_backend.Models;
using PROTOTYPE_backend.DTOs.Workspace;

namespace PROTOTYPE_backend.Services.Workspaces
{
    public interface IWorkspaceService
    {
        //POST /workspace
        Task<CreateWorkspaceDto> CreateWorkspaceAsync(Guid userId, CreateWorkspaceDto dto);

        //GET /workspace/user/{userId}
        Task<List<UsersWorkspaceListResponseDto>> GetUserWorkspacesAsync(Guid userId);
        Task<WorkspaceDataResponseDto?> GetWorkspaceDataAsync(Guid WorkspaceId);
        Task<List<WorkspaceProjectResponseDto>> GetWorkspaceProjectAsync(Guid WorkspaceId, int Page = 1, int Limit = 20,sbyte? Status = null);
        Task<List<WorkspaceMembersResponseDto>> GetWorkspaceMembersAsync(Guid WorkspaceId, int Page = 1, int Limit = 50, Guid? Role = null);
        Task<Project> CreateProjectAsync(Guid workspaceId, string name, string? description, DateTime? deadline, Guid? ownerId);
        Task<string> SwitchWorkspaceAsync(Guid userId, Guid workspaceId);
    }
}