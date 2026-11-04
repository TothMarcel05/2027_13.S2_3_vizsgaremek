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
        
        //GET /workspace/workspaces/{workspaceId}
        Task<WorkspaceDataResponseDto?> GetWorkspaceDataAsync(Guid WorkspaceId);

        //GET /workspace/workspaces/{workspaceId}/projects
        Task<List<WorkspaceProjectResponseDto>> GetWorkspaceProjectAsync(Guid WorkspaceId, int Page = 1, int Limit = 20,sbyte? Status = null);

        //GET /workspace/workspaces/{workspaceId}/members
        Task<List<WorkspaceMembersResponseDto>> GetWorkspaceMembersAsync(Guid WorkspaceId, int Page = 1, int Limit = 50, Guid? Role = null);

        //POST /workspace/workspaces/{workspaceId}/project
        Task<CreateProjectDto> CreateProjectAsync(Guid workspaceId, string name, string? description, DateOnly? deadline, Guid? ownerId);

        //POST /workspace/workspaces/{workspaceId}/switch
        Task<string> SwitchWorkspaceAsync(Guid userId, Guid workspaceId);
    }
}