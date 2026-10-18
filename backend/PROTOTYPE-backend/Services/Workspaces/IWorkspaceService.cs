using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PROTOTYPE_backend.Models;

namespace PROTOTYPE_backend.Services.Workspaces
{
    public interface IWorkspaceService
    {
        Task<IEnumerable<Workspace>> GetUserWorkspacesAsync(Guid userId);
        Task<Workspace> CreateWorkspaceAsync(Guid userId, string name);
        Task<Project> CreateProjectAsync(Guid workspaceId, string name, string? description, DateTime? deadline, Guid? ownerId);
        Task<string> SwitchWorkspaceAsync(Guid userId, Guid workspaceId);
    }
}