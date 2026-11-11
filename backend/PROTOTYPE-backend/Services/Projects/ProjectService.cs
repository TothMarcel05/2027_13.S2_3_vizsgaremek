using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using PROTOTYPE_backend.Data;
using PROTOTYPE_backend.DTOs.Project;
using PROTOTYPE_backend.DTOs.Workspace;
using PROTOTYPE_backend.Models;
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


        public async Task<List<ProjectResponseDto>> GetWorkspaceByIdAsync(Guid workspaceId)
        {
            var workspace = await _context.Projects.AnyAsync(w => w.WorkspaceId == workspaceId);
            if (!workspace) throw new Exception("Nem létezik a munkaterület!");

            return await _context.Projects
                .Where(w => w.WorkspaceId == workspaceId)
                .Select(w => new ProjectResponseDto(w.Id, w.Name, w.WorkspaceId))
                .ToListAsync();
        }

        public async Task<ProjectDataDto?> GetProjectByIdAsync(Guid projectId)
        {
            return await _context.Projects
                .Where(p => p.Id == projectId)
                .Select(p => new ProjectDataDto(
                    p.Name,                  
                    p.Description,
                    p.Status,
                    p.Deadline,
                    p.OwnerId,
                    p.CreatedAt,
                    p.UpdatedAt
                ))
                .FirstOrDefaultAsync();
        }




        public async Task<List<ProjectTaskListDto>> GetProjectTasksAsync(Guid projectId, sbyte? status = null, byte? priority = null)
        {
            var query = _context.ProjectTasks.Where(t => t.ProjectId == projectId);

            if (status.HasValue)
            {
                query = query.Where(t => t.Status == status.Value);
            }

            if (priority.HasValue)
            {
                query = query.Where(t => t.Priority == (sbyte)priority.Value);
            }

            return await query
                .Select(t => new ProjectTaskListDto(
                    t.Id,
                    t.ProjectId,
                    t.Title,
                    t.Description,
                    t.Priority,
                    t.Status,
                    t.DueDate,
                    t.AssigneeUserId,
                    t.CreatedAt
                ))
                .ToListAsync();
        }

        public async Task<ProjectTaskResponseDto?> GetProjectTaskAsync(Guid taskId)
        {
            return await _context.ProjectTasks
                .Where(p => p.Id == taskId)
                .Select(t => new ProjectTaskResponseDto(
                    t.Id,
                    t.ProjectId,
                    t.Title,
                    t.Description,
                    t.Priority,
                    t.Status,
                    t.DueDate,
                    t.AssigneeUserId,
                    t.CreatedAt
                ))
                .FirstOrDefaultAsync();
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

        public async Task<ProjectDataDto?> UpdateProjectAsync(Guid projectId, UpdateProjectDto dto)
        {
            var project = await _context.Projects.FindAsync(projectId);
            if (project == null) return null;

            project.Name = dto.Name;
            project.Description = dto.Description;
            project.Status = dto.Status;
            project.Deadline = dto.Deadline;
            project.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new ProjectDataDto(
                project.Name,
                project.Description,
                project.Status,
                project.Deadline,
                project.OwnerId,
                project.CreatedAt,
                project.UpdatedAt
            );
        }

        public async Task<bool> DeleteProjectAsync(Guid projectId)
        {
            var project = await _context.Projects.FindAsync(projectId);
            if (project == null) return false;

            _context.Projects.Remove(project);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<ProjectTaskResponseDto?> UpdateTaskAsync(Guid taskId, UpdateTaskDto dto)
        {
            var task = await _context.ProjectTasks.FindAsync(taskId);
            if (task == null) return null;

            task.Title = dto.Title;
            task.Description = dto.Description;
            task.Priority = dto.Priority;
            task.Status = dto.Status;
            task.DueDate = dto.DueDate;
            task.AssigneeUserId = dto.AssigneeUserId;
            task.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new ProjectTaskResponseDto(
                task.Id,
                task.ProjectId,
                task.Title,
                task.Description,
                task.Priority,
                task.Status,
                task.DueDate,
                task.AssigneeUserId,
                task.CreatedAt
            );
        }

        public async Task<bool> DeleteTaskAsync(Guid taskId)
        {
            var task = await _context.ProjectTasks.FindAsync(taskId);
            if (task == null) return false;

            _context.ProjectTasks.Remove(task);
            await _context.SaveChangesAsync();
            return true;
        }

    }
}
