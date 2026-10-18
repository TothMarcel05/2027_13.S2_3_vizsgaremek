using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PROTOTYPE_backend.Data;
using PROTOTYPE_backend.Models;

namespace PROTOTYPE_backend.Services.Workspace
{
    public class WorkspaceService : IWorkspaceService
    {
        private readonly AppDbContext _context;

        public WorkspaceService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<UserWorkspace>> GetUserWorkspacesAsync(string userId)
        {
            return await _context.UserWorkspaces
                .Where(w => w.OwnerId == userId || w.WorkspaceMembers.Any(m => m.UserId == userId))
                .Include(w => w.AppProjects)
                .ToListAsync();
        }

        public async Task<UserWorkspace> CreateWorkspaceAsync(string userId, string name)
        {
            var workspace = new UserWorkspace
            {
                Name = name,
                OwnerId = userId,
                CreatedAt = DateTime.UtcNow
            };

            _context.UserWorkspaces.Add(workspace);
            await _context.SaveChangesAsync();
            return workspace;
        }

        public async Task<AppProject> CreateProjectAsync(Guid workspaceId, string name, string? description, DateTime? deadline, string? ownerId)
        {
            var project = new AppProject
            {
                WorkspaceId = workspaceId,
                Name = name,
                Description = description,
                Deadline = deadline,
                OwnerId = ownerId,
                Status = 1, // Planned
                CreatedAt = DateTime.UtcNow
            };

            _context.AppProjects.Add(project);
            await _context.SaveChangesAsync();
            return project;
        }

        public async Task<ProjectTask> CreateTaskAsync(Guid projectId, string title, string? description, byte priority, DateTime? dueDate, string? assigneeId, string? reporterId)
        {
            var task = new ProjectTask
            {
                ProjectId = projectId,
                Title = title,
                Description = description,
                Priority = priority == 0 ? (byte)2 : priority, 
                Status = 1,
                DueDate = dueDate,
                AssigneeUserId = assigneeId,
                ReporterId = reporterId,
                CreatedAt = DateTime.UtcNow
            };

            _context.ProjectTasks.Add(task);
            await _context.SaveChangesAsync();
            return task;
        }

        public async Task<bool> UpdateTaskStatusAsync(Guid taskId, byte newStatus)
        {
            var task = await _context.ProjectTasks.FindAsync(taskId);
            if (task == null) return false;

            task.Status = newStatus;
            task.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }
    }
}