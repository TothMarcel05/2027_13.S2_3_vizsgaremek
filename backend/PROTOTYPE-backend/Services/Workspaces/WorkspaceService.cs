using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
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
            _tokenService = tokenService ;
        }

        public async Task<Workspace> CreateWorkspaceAsync(Guid userId, string WorkspaceName)
        {
            var existingWorkspace = await _context.Workspaces
                .FirstOrDefaultAsync(w => w.Name == WorkspaceName && w.OwnerId == userId);

            var workspace = new Workspace
            {
                Name = WorkspaceName,
                OwnerId = userId,
                CreatedAt = DateTime.UtcNow,
            };

            _context.Workspaces.Add(workspace);
            await _context.SaveChangesAsync();
            return workspace;
        }

        public async Task<WorkspaceDataResponseDto?> GetWorkspaceDataAsync(Guid WorkspaceId)
        {
            return await _context.Workspaces
                .Where(w => w.Id == WorkspaceId)
                .Select(w => new WorkspaceDataResponseDto(
                    w.Id,
                    w.Name,
                    w.OwnerId,
                    w.Owner.Name,
                    w.StorageQuotaBytes
                ))
                .FirstOrDefaultAsync();
        }

        public async Task<List<WorkspaceProjectResponseDto>> GetWorkspaceProjectAsync(Guid WorkspaceId, int Page = 1, int Limit = 20, sbyte? Status = null) {
            var query = _context.Projects
                .Where(p => p.WorkspaceId == WorkspaceId);

            if (Status.HasValue)
            {
                query = query.Where(p => p.Status == Status.Value);
            }
            return await query
                .Skip((Page - 1) * Limit)
                .Take(Limit)
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

        public async Task<IEnumerable<Workspace>> GetUserWorkspacesAsync(Guid userId)
        {
            var user  = _context.AppUsers.FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                throw new Exception("Nem létezik a felhasználó!");
            }


            return await _context.Workspaces
                .Where(w => w.OwnerId == userId || w.WorkspaceMembers.Any(m => m.UserId == userId))
                .ToListAsync();
        }



        public async Task<Project> CreateProjectAsync(Guid workspaceId, string name, string? description, DateTime? deadline, Guid? ownerId)
        {
            var project = new Project
            {
                WorkspaceId = workspaceId,
                Name = name,
                Description = description,
                Deadline = deadline.HasValue ? DateOnly.FromDateTime(deadline.Value) : null,
                OwnerId = ownerId,
                Status = 1, 
                CreatedAt = DateTime.UtcNow
            };

            _context.Projects.Add(project);
            await _context.SaveChangesAsync();
            return project;
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
    }
}