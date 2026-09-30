using Microsoft.EntityFrameworkCore;
using PROTOTYPE_backend.DTOs.Workspace;
using PROTOTYPE_backend.Models;

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
            var res = await _context.UserProjectRole
                .AsNoTracking()
                .Where(pm => pm.UserId == userId && pm.ProjectId == projectId)
                .Select(pm => new
                {
                    ProjectId = pm.ProjectId,
                    RoleName = pm.Roles
                }
                ).ToListAsync();
        }
    }
}
