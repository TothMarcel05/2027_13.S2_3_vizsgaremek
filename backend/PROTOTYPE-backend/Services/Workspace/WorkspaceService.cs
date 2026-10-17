using Microsoft.EntityFrameworkCore;
using PROTOTYPE_backend.Data;
using PROTOTYPE_backend.Data.Repositories;
using PROTOTYPE_backend.DTOs.Workspace;
using PROTOTYPE_backend.Models;
using PROTOTYPE_backend.Services.Token;

namespace PROTOTYPE_backend.Services.Workspace
{
    public class WorkspaceService : IWorkspaceService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITokenService _tokenService;
        private readonly AppDbContext _context;

        public WorkspaceService(IUnitOfWork unitOfWork, ITokenService tokenService, AppDbContext context)
        {
            _tokenService = tokenService;
            _unitOfWork = unitOfWork;
            _context = context;
        }

        public async Task<SelectWorkspaceResponseDto> SelectWorkspaceAsync(Guid userId, Guid workspaceId)
        {
            var user = await _unitOfWork.Repository<AppUser>().FirstOrDefaultAsync(u => u.Id == userId);
            var workspace = await _unitOfWork.Repository<UserWorkspace>().FirstOrDefaultAsync(u => u.Id == workspaceId);

            if (user == null) throw new Exception("A felhasználó nem található.");
            if (workspace == null) throw new Exception("A munkaterület nem található.");

            var rolesAndPermissions = await _context.Roles
                .AsNoTracking()
                .Where(r => r.WorkspaceId == workspaceId)
                .Select(r => new
                {
                    RoleName = r.Name,
                    Permissions = r.Permissions.Select(p => p.Code).ToList(),
                })
                .ToListAsync();

            var roles = rolesAndPermissions.Select(rap => rap.RoleName).ToList();
            var permissions = rolesAndPermissions.SelectMany(r => r.Permissions).Distinct().ToList();

            var newAccessToken = _tokenService.GenerateWorkspaceToken(userId, workspaceId, roles, permissions);

            return new SelectWorkspaceResponseDto(newAccessToken, workspaceId, workspace.Name);
        }


        public async Task<WorkspaceDto?> CreateWorkspaceAsync(CreateWorkspaceDto dto, Guid ownerId)
        {
            var workspace = new UserWorkspace
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                OwnerId = ownerId,
                CreatedAt = DateTime.UtcNow
            };

            _context.UserWorkspaces.Add(workspace);
            await _context.SaveChangesAsync();

            return new WorkspaceDto(workspace.Id, workspace.Name, new List<ProjectDto>());
        }

        public async Task<ProjectDto?> CreateProjectAsync(CreateProjectDto dto, Guid userId)
        {
            var project = new AppProject
            {
                Id = Guid.NewGuid(),
                WorkspaceId = dto.WorkspaceId,
                Name = dto.Name,
                Description = dto.Description,
                Deadline = dto.Deadline,
                Status = "planned",
                CreatedAt = DateTime.UtcNow
            };

            _context.AppProjects.Add(project);
            await _context.SaveChangesAsync();

            return new ProjectDto(project.Id, project.Name, project.Description, project.Status, project.Deadline, new List<TaskDto>());
        }

        public async Task<TaskDto?> CreateTaskAsync(CreateTaskDto dto, Guid reporterId)
        {
            var task = new ProjectTask
            {
                Id = Guid.NewGuid(),
                ProjectId = dto.ProjectId,
                ReporterId = reporterId,
                Title = dto.Title,
                Description = dto.Description,
                Priority = string.IsNullOrEmpty(dto.Priority) ? "medium" : dto.Priority,
                Status = "Tervezés",
                DueDate = dto.DueDate,
                AssigneeId = dto.AssigneeId,
                CreatedAt = DateTime.UtcNow
            };

            _context.ProjectTasks.Add(task);
            await _context.SaveChangesAsync();

            return new TaskDto(task.Id, task.Title, task.Description, task.Status, task.Priority, task.DueDate, task.AssigneeId);
        }

        public async Task<bool> UpdateTaskStatusAsync(UpdateTaskStatusDto dto, Guid userId)
        {
            var task = await _context.ProjectTasks.FindAsync(dto.TaskId);
            if (task == null) return false;

            var validStatuses = new[] { "Tervezés", "Folyamatban", "Kész" };
            if (!validStatuses.Contains(dto.Status)) return false;

            task.Status = dto.Status;
            task.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<WorkspaceDto>> GetUserWorkspacesAsync(Guid userId)
        {
            var workspaces = await _context.UserWorkspaces
                .AsNoTracking()
                .Where(w => w.OwnerId == userId)
                .Include(w => w.AppProjects)
                    .ThenInclude(p => p.ProjectTasks)
                .ToListAsync();

            return workspaces.Select(w => new WorkspaceDto(
                w.Id,
                w.Name,
                w.AppProjects.Select(p => new ProjectDto(
                    p.Id,
                    p.Name,
                    p.Description ?? "",
                    p.Status,
                    p.Deadline,
                    p.ProjectTasks.Select(t => new TaskDto(
                        t.Id, t.Title, t.Description ?? "", t.Status, t.Priority, t.DueDate, t.AssigneeId
                    )).ToList()
                )).ToList()
            )).ToList();
        }
    }
}