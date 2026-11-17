using Microsoft.EntityFrameworkCore;
using PROTOTYPE_backend.Data;
using PROTOTYPE_backend.DTOs.Admin;
using PROTOTYPE_backend.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PROTOTYPE_backend.Services.Admin;

public class AdminService : IAdminService
{
    private readonly AppDbContext _context;

    public AdminService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<UserRoleResponseDto>> GetRolesAndPermissionsAsync()
    {
        return await _context.AppRoles
            .Select(r => new UserRoleResponseDto(
                r.Id,
                r.Name,
                r.Permissions.Select(p => p.Code).ToList()
            ))
            .ToListAsync();
    }

    public async Task<UserRoleResponseDto> CreateRoleAsync(AdminCreateRoleDto dto)
    {
        if (await _context.AppRoles.AnyAsync(r => r.Name == dto.Name))
        {
            throw new InvalidOperationException("Ez a szerepkör név már foglalt.");
        }

        var newRole = new AppRole
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            IsSystem = false, 
            CreatedAt = DateTime.UtcNow,
            Permissions = new List<Permission>()
        };

        if (dto.Permissions != null && dto.Permissions.Any())
        {
            var permissions = await _context.Permissions
                .Where(p => dto.Permissions.Contains(p.Code))
                .ToListAsync();

            newRole.Permissions = permissions;
        }

        _context.AppRoles.Add(newRole);
        await _context.SaveChangesAsync();

        return new UserRoleResponseDto(
            newRole.Id,
            newRole.Name,
            newRole.Permissions.Select(p => p.Code).ToList()
        );
    }

    public async Task<bool> UpdateRolePermissionsAsync(Guid roleId, UpdateRolePermissionsDto dto)
    {
        var role = await _context.AppRoles
            .Include(r => r.Permissions)
            .FirstOrDefaultAsync(r => r.Id == roleId);

        if (role == null) return false;

        var newPermissions = await _context.Permissions
            .Where(p => dto.Permissions.Contains(p.Code))
            .ToListAsync();

        role.Permissions.Clear();
        foreach (var permission in newPermissions)
        {
            role.Permissions.Add(permission);
        }

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<IEnumerable<UserAdminViewDto>> GetAllUsersAsync(int page = 1, string? search = null)
    {
        var query = _context.AppUsers.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(u => u.Name.ToLower().Contains(searchLower) || u.Email.ToLower().Contains(searchLower));
        }

        int pageSize = 20; 

        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
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

    public async Task<bool> UpdateUserGlobalRoleAsync(Guid userId, UpdateUserGlobalRoleDto dto)
    {
        var user = await _context.AppUsers.FindAsync(userId);
        if (user == null) return false;

        user.GlobalRole = dto.NewGlobalRole;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<SystemOverviewStatsDto> GetSystemStatsAsync()
    {
        return new SystemOverviewStatsDto(
            await _context.AppUsers.CountAsync(),
            await _context.AppUsers.CountAsync(u => u.DeletedAt == null),
            await _context.Workspaces.CountAsync(),
            await _context.Projects.CountAsync(),
            await _context.ProjectTasks.CountAsync()
        );
    }

    public async Task<bool> ToggleUserStatusAsync(Guid userId)
    {
        var user = await _context.AppUsers.FindAsync(userId);
        if (user == null) return false;

        user.DeletedAt = user.DeletedAt == null ? DateTime.UtcNow : null;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RemoveUserAsync(Guid userId)
    {
        var user = await _context.AppUsers.FindAsync(userId);
        if (user == null) return false;

        _context.AppUsers.Remove(user);
        await _context.SaveChangesAsync();
        return true;
    }
}