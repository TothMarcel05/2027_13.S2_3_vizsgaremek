using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PROTOTYPE_backend.Models;
using PROTOTYPE_backend.DTOs.Workspace;

namespace PROTOTYPE_backend.Services.Workspaces
{
    public interface IWorkspaceService
    {
        Task<IEnumerable<Workspace>> GetUserWorkspacesAsync(Guid userId);
        Task<Workspace> CreateWorkspaceAsync(Guid userId, string WorkspaceName);
        Task<WorkspaceDataResponseDto?> GetWorkspaceDataAsync(Guid WorkspaceId);
        Task<List<WorkspaceProjectResponseDto>> GetWorkspaceProjectAsync(Guid WorkspaceId, int Page = 1, int Limit = 20,sbyte? Status = null);
        Task<List<WorkspaceMembersResponseDto>> GetWorkspaceMembersAsync(Guid WorkspaceId, int Page = 1, int Limit = 50, Guid? Role = null);
        Task<Project> CreateProjectAsync(Guid workspaceId, string name, string? description, DateTime? deadline, Guid? ownerId);
        Task<string> SwitchWorkspaceAsync(Guid userId, Guid workspaceId);
    }
}