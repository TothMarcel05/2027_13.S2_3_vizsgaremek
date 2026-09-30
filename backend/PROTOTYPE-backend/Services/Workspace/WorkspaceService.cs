using PROTOTYPE_backend.Data.Repositories;
using PROTOTYPE_backend.DTOs.Workspace;
using PROTOTYPE_backend.Services.Token;
using PROTOTYPE_backend.Models;
using PROTOTYPE_backend.Data;
using Microsoft.EntityFrameworkCore;

namespace PROTOTYPE_backend.Services.Workspace
{
    public class WorkspaceService :IWorkspaceService
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

        async Task<SelectWorkspaceResponseDto> SelectWorkspaceAsync(Guid userId, Guid workspaceId) 
        {
            var user = await _unitOfWork.Repository<AppUser>().FirstOrDefaultAsync(u => u.Id == userId);
            var workspace = await _unitOfWork.Repository<UserWorkspace>().FirstOrDefaultAsync(u => u.Id == workspaceId);

            if (user == null) throw new Exception("");

            if (workspace == null) throw new Exception("");

            var rolesAndPermissions = await _context.Roles
                .AsNoTracking()
                .Where(r => r.WorkspaceId == workspaceId)
                .Select(r => new
                    {
                        RoleName = r.Name,
                        Permissions = r.Permissions.Select(p => p.Code).ToList(),
                    }
                ).ToListAsync();

            var role = rolesAndPermissions.Select(rap => rap.RoleName);
            var permission = rolesAndPermissions.SelectMany(r => r.Permissions);

            var newAccessToken = _tokenService.GenerateWorkspaceToken
                (
                    userId,
                    workspaceId,
                    
                );
        }
    }
}
