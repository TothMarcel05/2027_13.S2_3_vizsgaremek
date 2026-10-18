using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PROTOTYPE_backend.Models;

namespace PROTOTYPE_backend.Services.Workspace
{
    public interface IWorkspaceService
    {
        Task<IEnumerable<UserWorkspace>> GetUserWorkspacesAsync(string userId);
        Task<UserWorkspace> CreateWorkspaceAsync(string userId, string name);
        Task<AppProject> CreateProjectAsync(Guid workspaceId, string name, string? description, DateTime? deadline, string? ownerId);
        Task<ProjectTask> CreateTaskAsync(Guid projectId, string title, string? description, byte priority, DateTime? dueDate, string? assigneeId, string? reporterId);
        Task<bool> UpdateTaskStatusAsync(Guid taskId, byte newStatus);
    }
}