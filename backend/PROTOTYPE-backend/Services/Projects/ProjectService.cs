using Microsoft.EntityFrameworkCore;
using PROTOTYPE_backend.Models;
using PROTOTYPE_backend.Data;
using System;
using System.Threading.Tasks;

namespace PROTOTYPE_backend.Services.Projects
{
    public class ProjectService
    {
        private readonly AppDbContext _context;

        public ProjectService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ProjectTask> CreateTaskAsync(Guid projectId, string title, string? description, byte priority, DateTime? dueDate, Guid? assigneeId)
        {
            var task = new ProjectTask
            {
                ProjectId = projectId,
                Title = title,
                Description = description,
                Priority = (sbyte)priority,
                Status = 1,
                DueDate = dueDate.HasValue ? DateOnly.FromDateTime(dueDate.Value) : (DateOnly?)null,
                AssigneeUserId = assigneeId,
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

            task.Status = (sbyte)newStatus;
            task.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

    }
}
