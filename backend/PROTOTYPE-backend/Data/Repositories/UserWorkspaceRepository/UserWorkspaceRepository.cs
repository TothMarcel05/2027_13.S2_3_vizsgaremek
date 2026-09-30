using Microsoft.EntityFrameworkCore;
using System.Linq;
using PROTOTYPE_backend.DTOs.Workspace;
using PROTOTYPE_backend.Models;
using PROTOTYPE_backend.Data;

namespace PROTOTYPE_backend.Data.Repositories.UserWorkspaceRepository
{
    public class UserWorkspaceRepository : IUserWorkspaceRepository
    {
        private readonly AppDbContext _context;

        public UserWorkspaceRepository(AppDbContext context) 
        {
            _context = context;
        }

        public async Task<string?> GetUserRoleInWorkspaceAsync(Guid userId, Guid workspaceId)
        {
            var result = await _context.WorkspaceMembers
                .AsNoTracking()
                .Where(wm => wm.UserId == userId && wm.WorkspaceId == workspaceId)
                .SelectMany(wm => wm.Roles)
                .Select(r => r.Name)
                .ToListAsync();

            if(!result.Any()) return null;

            return string.Join(',', result);
        }

        public async Task<Dictionary<string, List<string>>> GetUserRolesInProjectAsync(Guid userId, Guid projectId)
        {
            var roleNames = await _context.ProjectMembers
                .AsNoTracking()
                .Where(pm => pm.UserId == userId && pm.ProjectId == projectId)
                .SelectMany(pm => pm.Roles.Select(r => r.Name)) 
                .ToListAsync();

            return new Dictionary<string, List<string>>
            {
                { projectId.ToString(), roleNames }
            };
        }
    }
}
