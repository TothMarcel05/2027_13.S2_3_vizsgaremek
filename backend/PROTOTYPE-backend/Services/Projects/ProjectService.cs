using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PROTOTYPE_backend.Data;
using PROTOTYPE_backend.DTOs.Project;
using PROTOTYPE_backend.DTOs.Workspace;
using PROTOTYPE_backend.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PROTOTYPE_backend.Services.Projects
{
    public class ProjectService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ProjectService> _logger;

        public ProjectService(AppDbContext context, ILogger<ProjectService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<ProjectResponseDto>> GetWorkspaceByIdAsync(Guid workspaceId)
        {
            var workspace = await _context.Projects.AnyAsync(w => w.WorkspaceId == workspaceId);
            if (!workspace)
            {
                _logger.LogWarning("Projektek lekérdezése sikertelen: nem létező vagy üres munkaterület. WorkspaceId: {WorkspaceId}", workspaceId);
                throw new Exception("Nem létezik a munkaterület!");
            }

            return await _context.Projects
                .AsNoTracking()
                .Where(w => w.WorkspaceId == workspaceId)
                .Select(w => new ProjectResponseDto(w.Id, w.Name, w.WorkspaceId))
                .ToListAsync();
        }

        public async Task<ProjectDataDto?> GetProjectByIdAsync(Guid projectId)
        {
            var project = await _context.Projects
                .AsNoTracking()
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

            if (project == null)
            {
                _logger.LogDebug("Projekt lekérdezése: nem található rekord. ProjectId: {ProjectId}", projectId);
            }

            return project;
        }

        public async Task<List<ProjectTaskListDto>> GetProjectTasksAsync(Guid projectId, sbyte? status = null, byte? priority = null)
        {
            var query = _context.ProjectTasks
                .AsNoTracking()
                .Where(t => t.ProjectId == projectId);

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
            var task = await _context.ProjectTasks
                .AsNoTracking()
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

            if (task == null)
            {
                _logger.LogDebug("Feladat lekérdezése: nem található rekord. TaskId: {TaskId}", taskId);
            }

            return task;
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

            _logger.LogInformation(
                "Új projektfeladat létrehozva. TaskId: {TaskId}, ProjectId: {ProjectId}, Priority: {Priority}, AssigneeUserId: {AssigneeUserId}",
                task.Id, projectId, task.Priority, assigneeId);

            return task;
        }

        public async Task<bool> UpdateTaskStatusAsync(Guid taskId, byte newStatus)
        {
            var task = await _context.ProjectTasks.FindAsync(taskId);
            if (task == null)
            {
                _logger.LogWarning("Feladat státuszának frissítése sikertelen: a feladat nem található. TaskId: {TaskId}", taskId);
                return false;
            }

            var oldStatus = task.Status;
            task.Status = (sbyte)newStatus;
            task.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Feladat státusza módosítva. TaskId: {TaskId}, ProjectId: {ProjectId}, RégiStátusz: {OldStatus}, ÚjStátusz: {NewStatus}",
                task.Id, task.ProjectId, oldStatus, task.Status);

            return true;
        }

        public async Task<ProjectDataDto?> UpdateProjectAsync(Guid projectId, UpdateProjectDto dto)
        {
            var project = await _context.Projects.FindAsync(projectId);
            if (project == null)
            {
                _logger.LogWarning("Projekt módosítása sikertelen: a projekt nem található. ProjectId: {ProjectId}", projectId);
                return null;
            }

            var oldStatus = project.Status;
            project.Name = dto.Name;
            project.Description = dto.Description;
            project.Status = dto.Status;
            project.Deadline = dto.Deadline;
            project.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Projekt adatai frissítve. ProjectId: {ProjectId}, WorkspaceId: {WorkspaceId}, RégiStátusz: {OldStatus}, ÚjStátusz: {NewStatus}, Deadline: {Deadline}",
                project.Id, project.WorkspaceId, oldStatus, project.Status, project.Deadline);

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
            if (project == null)
            {
                _logger.LogWarning("Projekt törlése sikertelen: a projekt nem található. ProjectId: {ProjectId}", projectId);
                return false;
            }

            var workspaceId = project.WorkspaceId;
            _context.Projects.Remove(project);
            await _context.SaveChangesAsync();

            _logger.LogWarning("Projekt véglegesen törölve. ProjectId: {ProjectId}, WorkspaceId: {WorkspaceId}", projectId, workspaceId);

            return true;
        }

        public async Task<ProjectTaskResponseDto?> UpdateTaskAsync(Guid taskId, UpdateTaskDto dto)
        {
            var task = await _context.ProjectTasks.FindAsync(taskId);
            if (task == null)
            {
                _logger.LogWarning("Feladat módosítása sikertelen: a feladat nem található. TaskId: {TaskId}", taskId);
                return null;
            }

            task.Title = dto.Title;
            task.Description = dto.Description;
            task.Priority = dto.Priority;
            task.Status = dto.Status;
            task.DueDate = dto.DueDate;
            task.AssigneeUserId = dto.AssigneeUserId;
            task.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Projektfeladat módosítva. TaskId: {TaskId}, ProjectId: {ProjectId}, Státusz: {Status}, Prioritás: {Priority}, AssigneeUserId: {AssigneeUserId}",
                task.Id, task.ProjectId, task.Status, task.Priority, task.AssigneeUserId);

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
            if (task == null)
            {
                _logger.LogWarning("Feladat törlése sikertelen: a feladat nem található. TaskId: {TaskId}", taskId);
                return false;
            }

            var projectId = task.ProjectId;
            _context.ProjectTasks.Remove(task);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Projektfeladat törölve. TaskId: {TaskId}, ProjectId: {ProjectId}", taskId, projectId);

            return true;
        }
    }
}