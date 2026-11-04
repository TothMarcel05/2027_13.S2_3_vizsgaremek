using Microsoft.EntityFrameworkCore;
using PROTOTYPE_backend.Data;
using PROTOTYPE_backend.DTOs.Workspace;
using PROTOTYPE_backend.Models;
using PROTOTYPE_backend.Services.Token;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PROTOTYPE_backend.Services.Workspaces
{
    public class WorkspaceService : IWorkspaceService
    {
        private readonly AppDbContext _context;
        private readonly ITokenService _tokenService;

        public WorkspaceService(AppDbContext context, ITokenService tokenService)
        {
            _context = context;
            _tokenService = tokenService;
        }

        public async Task<WorkspaceDataResponseDto> CreateWorkspaceAsync(Guid userId, CreateWorkspaceDto dto)
        {
            var existingWorkspace = await _context.Workspaces
                .FirstOrDefaultAsync(w => w.Name == dto.workspaceName && w.OwnerId == userId);

            if (existingWorkspace != null)
            {
                throw new Exception("Már létezik ilyen nevű munkaterület ehhez a felhasználóhoz!");
            }

            var workspace = new Workspace
            {
                Name = dto.workspaceName,
                OwnerId = userId,
                CreatedAt = DateTime.UtcNow,
                Description = dto.description,
                StorageQuotaBytes = 0 
            };

            _context.Workspaces.Add(workspace);
            await _context.SaveChangesAsync();

            var user = await _context.AppUsers.FirstOrDefaultAsync(u => u.Id == userId);

            return new WorkspaceDataResponseDto(
                workspace.Id,
                workspace.Name,
                workspace.OwnerId,
                user?.Name ?? "Ismeretlen",
                workspace.StorageQuotaBytes
            );
        }

        public async Task<List<WorkspaceResponseDto>> GetUserWorkspacesAsync(Guid userId)
        {
            var userExists = await _context.AppUsers.AnyAsync(u => u.Id == userId);
            if (!userExists)
            {
                throw new Exception("Nem létezik a felhasználó!");
            }

            return await _context.Workspaces
                .Where(w => w.OwnerId == userId || w.WorkspaceMembers.Any(m => m.UserId == userId))
                .Select(w => new WorkspaceResponseDto(
                    w.Id,
                    w.Name,
                    w.OwnerId
                ))
                .ToListAsync();
        }

        public async Task<WorkspaceDataResponseDto?> GetWorkspaceDataAsync(Guid workspaceId)
        {
            return await _context.Workspaces
                .Where(w => w.Id == workspaceId)
                .Select(w => new WorkspaceDataResponseDto(
                    w.Id,
                    w.Name,
                    w.OwnerId,
                    w.Owner.Name,
                    w.StorageQuotaBytes
                ))
                .FirstOrDefaultAsync();
        }

        public async Task<List<WorkspaceProjectResponseDto>> GetWorkspaceProjectsAsync(Guid workspaceId, int page = 1, int limit = 20, sbyte? status = null)
        {
            var query = _context.Projects.Where(p => p.WorkspaceId == workspaceId);

            if (status.HasValue)
            {
                query = query.Where(p => p.Status == status.Value);
            }

            return await query
                .Skip((page - 1) * limit)
                .Take(limit)
                .Select(p => new WorkspaceProjectResponseDto(
                    p.WorkspaceId,
                    p.Id,
                    p.Name,
                    p.Status,
                    p.Description,
                    p.Deadline
                ))
                .ToListAsync();
        }

        public async Task<List<WorkspaceMemberResponseDto>> GetWorkspaceMembersAsync(Guid workspaceId, int page = 1, int limit = 50, Guid? role = null)
        {
            var query = _context.WorkspaceMembers.Where(p => p.WorkspaceId == workspaceId);

            if (role.HasValue)
            {
                query = query.Where(p => p.RoleId == role.Value);
            }

            return await query
                .Skip((page - 1) * limit)
                .Take(limit)
                .Select(p => new WorkspaceMemberResponseDto(
                    p.WorkspaceId,
                    p.UserId,
                    p.User.Name,
                    p.RoleId,
                    p.JoinedAt
                ))
                .ToListAsync();
        }

        public async Task<WorkspaceProjectResponseDto> CreateProjectAsync(Guid workspaceId, CreateProjectDto dto)
        {
            var project = new Project
            {
                WorkspaceId = workspaceId,
                Name = dto.name,
                Description = dto.description,
                Deadline = dto.deadline,
                OwnerId = dto.ownerId,
                Status = 1,
                CreatedAt = DateTime.UtcNow
            };

            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            return new WorkspaceProjectResponseDto(
                project.WorkspaceId,
                project.Id,
                project.Name,
                project.Status,
                project.Description,
                project.Deadline
            );
        }

        public async Task<string> SwitchWorkspaceAsync(Guid userId, Guid workspaceId)
        {
            var user = await _context.AppUsers
                .FirstOrDefaultAsync(u => u.Id == userId && u.DeletedAt == null)
                ?? throw new UnauthorizedAccessException("Érvénytelen felhasználó!");

            var membership = await _context.WorkspaceMembers
                .Where(m => m.UserId == userId && m.WorkspaceId == workspaceId)
                .Select(m => new
                {
                    RoleName = m.Role.Name,
                    Permissions = m.Role.Permissions.Select(p => p.Code).ToList()
                })
                .FirstOrDefaultAsync();

            if (membership == null)
                throw new Exception("Nincs hozzáférésed ehhez a munkaterülethez!");

            var rolesNPermissions = new Dictionary<string, List<string>>
            {
                [membership.RoleName] = membership.Permissions
            };

            return _tokenService.GenerateWorkspaceToken(user, workspaceId, rolesNPermissions);
        }

        public async Task<bool> DeleteWorkspaceAsync(Guid workspaceId)
        {
            var workspace = await _context.Workspaces.FindAsync(workspaceId);
            if (workspace == null)
                return false;

            _context.Workspaces.Remove(workspace);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}