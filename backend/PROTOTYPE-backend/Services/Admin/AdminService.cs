using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
    private readonly ILogger<AdminService> _logger;

    public AdminService(AppDbContext context, ILogger<AdminService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<UserRoleResponseDto>> GetRolesAndPermissionsAsync()
    {
        return await _context.AppRoles
            .AsNoTracking()
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
            _logger.LogWarning("Szerepkör létrehozása sikertelen: a név már foglalt. RoleName: {RoleName}", dto.Name);
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

            if (permissions.Count != dto.Permissions.Count)
            {
                _logger.LogWarning(
                    "Szerepkör létrehozásakor néhány megadott jogosultságkód nem létezik az adatbázisban. RoleName: {RoleName}, Kért: {RequestedCount}, Talált: {FoundCount}",
                    dto.Name, dto.Permissions.Count, permissions.Count);
            }

            newRole.Permissions = permissions;
        }

        _context.AppRoles.Add(newRole);
        await _context.SaveChangesAsync();

        var assignedCodes = newRole.Permissions.Select(p => p.Code).ToList();

        _logger.LogInformation(
            "Új szerepkör létrehozva. RoleId: {RoleId}, RoleName: {RoleName}, Jogosultságok: {@Permissions}",
            newRole.Id, newRole.Name, assignedCodes);

        return new UserRoleResponseDto(
            newRole.Id,
            newRole.Name,
            assignedCodes
        );
    }

    public async Task<bool> UpdateRolePermissionsAsync(Guid roleId, UpdateRolePermissionsDto dto)
    {
        var role = await _context.AppRoles
            .Include(r => r.Permissions)
            .FirstOrDefaultAsync(r => r.Id == roleId);

        if (role == null)
        {
            _logger.LogWarning("Szerepkör jogosultságainak frissítése sikertelen: a szerepkör nem található. RoleId: {RoleId}", roleId);
            return false;
        }

        var newPermissions = await _context.Permissions
            .Where(p => dto.Permissions.Contains(p.Code))
            .ToListAsync();

        if (newPermissions.Count != dto.Permissions.Count)
        {
            _logger.LogWarning(
                "Szerepkör frissítésekor érvénytelen jogosultságkódok is érkeztek. RoleId: {RoleId}, Kért: {RequestedCount}, Talált: {FoundCount}",
                roleId, dto.Permissions.Count, newPermissions.Count);
        }

        role.Permissions.Clear();
        foreach (var permission in newPermissions)
        {
            role.Permissions.Add(permission);
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Szerepkör jogosultságai frissítve. RoleId: {RoleId}, RoleName: {RoleName}, Új jogosultságok: {@Permissions}",
            role.Id, role.Name, newPermissions.Select(p => p.Code).ToList());

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
        if (user == null)
        {
            _logger.LogWarning("Globális szerepkör módosítása sikertelen: a felhasználó nem található. UserId: {UserId}", userId);
            return false;
        }

        var oldRole = user.GlobalRole;
        user.GlobalRole = dto.NewGlobalRole;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogWarning(
            "Felhasználó globális szerepköre megváltoztatva! UserId: {UserId}, Régi szerepkör: {OldRole}, Új szerepkör: {NewRole}",
            userId, oldRole, dto.NewGlobalRole);

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
        if (user == null)
        {
            _logger.LogWarning("Felhasználói státusz váltása sikertelen: a felhasználó nem található. UserId: {UserId}", userId);
            return false;
        }

        bool isSuspending = user.DeletedAt == null;
        user.DeletedAt = isSuspending ? DateTime.UtcNow : null;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Felhasználói fiók státusza módosítva. UserId: {UserId}, Új állapot: {NewStatus}",
            userId, isSuspending ? "Suspended" : "Active");

        return true;
    }

    public async Task<bool> RemoveUserAsync(Guid userId)
    {
        var user = await _context.AppUsers.FindAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("Felhasználó végleges törlése sikertelen: a felhasználó nem található. UserId: {UserId}", userId);
            return false;
        }

        _context.AppUsers.Remove(user);
        await _context.SaveChangesAsync();

        _logger.LogWarning("Felhasználó véglegesen törölve az adatbázisból (Hard Delete). UserId: {UserId}", userId);

        return true;
    }
}