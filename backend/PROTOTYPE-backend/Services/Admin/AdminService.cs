using Microsoft.EntityFrameworkCore;
using PROTOTYPE_backend.Data;
using PROTOTYPE_backend.DTOs.Admin;
using PROTOTYPE_backend.Models;
using PROTOTYPE_backend.Services.SoftDelete;

namespace PROTOTYPE_backend.Services.Admin;

public interface IAdminService
{
    Task<List<UserAdminDto>> GetAllUsersAsync();
    Task<bool> UpdateGlobalRoleAsync(UpdateGlobalRoleDto dto);
    Task<List<DeletedItemDto>> GetSoftDeletedItemsAsync();
    Task<bool> RestoreDeletedItemAsync(RestoreItemDto dto);
    Task<bool> PurgeExpiredDeletedItemsAsync(int retentionDays);
}

public class AdminService : IAdminService
{
    private readonly AppDbContext _context;
    private readonly ISoftDeleteService _softDeleteService;

    public AdminService(AppDbContext context, ISoftDeleteService softDeleteService)
    {
        _context = context;
        _softDeleteService = softDeleteService;
    }

    public async Task<List<UserAdminDto>> GetAllUsersAsync()
    {
        return await _context.AppUsers
            .AsNoTracking()
            .Select(u => new UserAdminDto(
                u.Id,
                u.Name,
                u.Email,
                u.GlobalRole,
                u.CreatedAt ?? DateTime.UtcNow
            ))
            .ToListAsync();
    }

    public async Task<bool> UpdateGlobalRoleAsync(UpdateGlobalRoleDto dto)
    {
        var user = await _context.AppUsers.FindAsync(dto.UserId);
        if (user == null) return false;

        var validRoles = new[] { "User", "SystemAdmin" };
        if (!validRoles.Contains(dto.NewGlobalRole)) return false;

        user.GlobalRole = dto.NewGlobalRole;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<DeletedItemDto>> GetSoftDeletedItemsAsync()
    {
        var deletedItems = new List<DeletedItemDto>();

        var deletedFiles = await _context.StorageNodes
            .IgnoreQueryFilters()
            .Where(f => f.DeletedAt != null)
            .Select(f => new DeletedItemDto(f.Id, f.Name, "StorageNode", f.DeletedAt, f.DeletedBy))
            .ToListAsync();

        var deletedTasks = await _context.ProjectTasks
            .IgnoreQueryFilters()
            .Where(t => t.DeletedAt != null)
            .Select(t => new DeletedItemDto(t.Id, t.Title, "ProjectTask", t.DeletedAt, t.DeletedBy))
            .ToListAsync();

        deletedItems.AddRange(deletedFiles);
        deletedItems.AddRange(deletedTasks);

        return deletedItems;
    }

    public async Task<bool> RestoreDeletedItemAsync(RestoreItemDto dto)
    {
        return dto.EntityType switch
        {
            "StorageNode" => await _softDeleteService.RestoreAsync<StorageNode>(dto.Id),
            "ProjectTask" => await _softDeleteService.RestoreAsync<ProjectTask>(dto.Id),
            "AppProject" => await _softDeleteService.RestoreAsync<AppProject>(dto.Id),
            "UserWorkspace" => await _softDeleteService.RestoreAsync<UserWorkspace>(dto.Id),
            _ => false
        };
    }

    public async Task<bool> PurgeExpiredDeletedItemsAsync(int retentionDays)
    {
        await _softDeleteService.HardDeleteExpiredAsync<StorageNode>(retentionDays);
        await _softDeleteService.HardDeleteExpiredAsync<ProjectTask>(retentionDays);
        await _softDeleteService.HardDeleteExpiredAsync<AppProject>(retentionDays);
        await _softDeleteService.HardDeleteExpiredAsync<UserWorkspace>(retentionDays);

        return true;
    }
}