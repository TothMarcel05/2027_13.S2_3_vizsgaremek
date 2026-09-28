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

        /*public async Task<UserWorkspaceRoleDto?> GetUserRoleInWorkspaceAsync(string userId, string workspaceId) 
        {
            return await _context.UserWorkspaces
                .AsNoTracking()
                .Include(uw => WorkspaceMember)
        }/*/
    }
}
