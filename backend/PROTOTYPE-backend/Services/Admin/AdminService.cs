using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PROTOTYPE_backend.Data;
using PROTOTYPE_backend.DTOs.Admin;

namespace PROTOTYPE_backend.Services.Admin
{
    public class AdminService : IAdminService
    {
        private readonly AppDbContext _context;

        public AdminService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<UserAdminViewDto>> GetAllUsersAsync()
        {
            var users = await _context.AppUsers
                .AsNoTracking()
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();

            return users.Select(u => new UserAdminViewDto(
                u.Id,
                u.Name,
                u.Email,
                u.GlobalRole,
                u.Is2faEnabled,
                u.CreatedAt,
                u.DeletedAt
            ));
        }

        public async Task<bool> UpdateUserGlobalRoleAsync(UpdateUserRoleDto dto)
        {
            var user = await _context.AppUsers.FindAsync(dto.UserId);
            if (user == null) return false;

            user.GlobalRole = dto.NewGlobalRole;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<SystemOverviewStatsDto> GetSystemStatsAsync()
        {
            var totalUsers = await _context.AppUsers.CountAsync();
            var activeUsers = await _context.AppUsers.CountAsync(u => u.DeletedAt == null);
            var totalWorkspaces = await _context.Workspaces.CountAsync();
            var totalProjects = await _context.Projects.CountAsync();
            var totalTasks = await _context.ProjectTasks.CountAsync();

            return new SystemOverviewStatsDto(
                totalUsers,
                activeUsers,
                totalWorkspaces,
                totalProjects,
                totalTasks
            );
        }

        public async Task<bool> ToggleUserStatusAsync(string userId)
        {
            var user = await _context.AppUsers.FindAsync(userId);
            if (user == null) return false;

            if (user.DeletedAt == null)
            {
                user.DeletedAt = DateTime.UtcNow;
            }
            else
            {
                user.DeletedAt = null;
            }

            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}