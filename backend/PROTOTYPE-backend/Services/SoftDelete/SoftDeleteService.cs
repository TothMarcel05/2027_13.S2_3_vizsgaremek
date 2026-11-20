using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PROTOTYPE_backend.Data;
using PROTOTYPE_backend.DTOs.SoftDelete;
using PROTOTYPE_backend.Models;

namespace PROTOTYPE_backend.Services.SoftDelete
{
    public class SoftDeleteService : ISoftDeleteService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<SoftDeleteService> _logger;
        private const int RetentionDays = 30;

        public SoftDeleteService(AppDbContext context, ILogger<SoftDeleteService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<bool> SoftDeleteAsync(SoftDeleteRequestDto dto)
        {
            if (dto.EntityName == "StorageNode")
            {
                if (!Guid.TryParse(dto.EntityId, out var nodeId))
                {
                    _logger.LogWarning("Soft delete sikertelen: érvénytelen StorageNode ID formátum. EntityId: {EntityId}, UserId: {UserId}", dto.EntityId, dto.UserId);
                    return false;
                }

                var node = await _context.StorageNodes.FindAsync(nodeId);
                if (node == null || node.DeletedAt != null)
                {
                    _logger.LogWarning("Soft delete sikertelen: StorageNode nem található vagy már törölve van. NodeId: {NodeId}, UserId: {UserId}", nodeId, dto.UserId);
                    return false;
                }

                node.DeletedAt = DateTime.UtcNow;
                node.UpdatedBy = dto.UserId;
            }
            else if (dto.EntityName == "AppUser")
            {
                if (!Guid.TryParse(dto.EntityId, out var targetUserId))
                {
                    _logger.LogWarning("Soft delete sikertelen: érvénytelen AppUser ID formátum. EntityId: {EntityId}, InitiatorUserId: {UserId}", dto.EntityId, dto.UserId);
                    return false;
                }

                var user = await _context.AppUsers.FindAsync(targetUserId);
                if (user == null || user.DeletedAt != null)
                {
                    _logger.LogWarning("Soft delete sikertelen: AppUser nem található vagy már törölve van. TargetUserId: {TargetUserId}, InitiatorUserId: {UserId}", targetUserId, dto.UserId);
                    return false;
                }

                user.DeletedAt = DateTime.UtcNow;
            }
            else if (dto.EntityName == "Workspace")
            {
                if (!Guid.TryParse(dto.EntityId, out var wsId)) return false;
                var ws = await _context.Workspaces.FindAsync(wsId);
                if (ws == null || ws.DeletedAt != null) return false;
                ws.DeletedAt = DateTime.UtcNow;
            }
            else if (dto.EntityName == "Project")
            {
                if (!Guid.TryParse(dto.EntityId, out var projId)) return false;
                var proj = await _context.Projects.FindAsync(projId);
                if (proj == null || proj.DeletedAt != null) return false;
                proj.DeletedAt = DateTime.UtcNow;
            }
            else
            {
                _logger.LogWarning("Soft delete elutasítva: nem támogatott entitástípus. EntityName: {EntityName}, UserId: {UserId}", dto.EntityName, dto.UserId);
                return false;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Entitás lomtárba helyezve (soft delete). EntityName: {EntityName}, EntityId: {EntityId}, InitiatorUserId: {UserId}",
                dto.EntityName, dto.EntityId, dto.UserId);

            return true;
        }

        public async Task<bool> RestoreAsync(RestoreRequestDto dto)
        {
            if (dto.EntityName == "StorageNode")
            {
                if (!Guid.TryParse(dto.EntityId, out var nodeId))
                {
                    _logger.LogWarning("Visszaállítás sikertelen: érvénytelen StorageNode ID formátum. EntityId: {EntityId}", dto.EntityId);
                    return false;
                }

                var node = await _context.StorageNodes.FindAsync(nodeId);
                if (node == null || node.DeletedAt == null)
                {
                    _logger.LogWarning("Visszaállítás sikertelen: StorageNode nem található vagy nincs törölve. NodeId: {NodeId}", nodeId);
                    return false;
                }

                if (node.DeletedAt.Value.AddDays(RetentionDays) < DateTime.UtcNow)
                {
                    _logger.LogWarning("Visszaállítás elutasítva: a {RetentionDays} napos megőrzési idő lejárt. NodeId: {NodeId}, DeletedAt: {DeletedAt}",
                        RetentionDays, nodeId, node.DeletedAt.Value);
                    return false;
                }

                node.DeletedAt = null;
            }
            else if (dto.EntityName == "AppUser")
            {
                if (!Guid.TryParse(dto.EntityId, out var targetUserId))
                {
                    _logger.LogWarning("Visszaállítás sikertelen: érvénytelen AppUser ID formátum. EntityId: {EntityId}", dto.EntityId);
                    return false;
                }

                var user = await _context.AppUsers.FindAsync(targetUserId);
                if (user == null || user.DeletedAt == null)
                {
                    _logger.LogWarning("Visszaállítás sikertelen: AppUser nem található vagy nincs törölve. TargetUserId: {TargetUserId}", targetUserId);
                    return false;
                }

                if (user.DeletedAt.Value.AddDays(RetentionDays) < DateTime.UtcNow)
                {
                    _logger.LogWarning("Visszaállítás elutasítva: a {RetentionDays} napos megőrzési idő lejárt. TargetUserId: {TargetUserId}, DeletedAt: {DeletedAt}",
                        RetentionDays, targetUserId, user.DeletedAt.Value);
                    return false;
                }

                user.DeletedAt = null;
            }

            else if (dto.EntityName == "Workspace")
            {
                if (!Guid.TryParse(dto.EntityId, out var wsId)) return false;
                var ws = await _context.Workspaces.FindAsync(wsId);
                if (ws == null || ws.DeletedAt == null || ws.DeletedAt.Value.AddDays(RetentionDays) < DateTime.UtcNow) return false;
                ws.DeletedAt = null;
            }
            else if (dto.EntityName == "Project")
            {
                if (!Guid.TryParse(dto.EntityId, out var projId)) return false;
                var proj = await _context.Projects.FindAsync(projId);
                if (proj == null || proj.DeletedAt == null || proj.DeletedAt.Value.AddDays(RetentionDays) < DateTime.UtcNow) return false;
                proj.DeletedAt = null;
            }

            else
            {
                _logger.LogWarning("Visszaállítás elutasítva: nem támogatott entitástípus. EntityName: {EntityName}", dto.EntityName);
                return false;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Entitás sikeresen visszaállítva a lomtárból. EntityName: {EntityName}, EntityId: {EntityId}",
                dto.EntityName, dto.EntityId);

            return true;
        }
        public async Task<int> HardDeleteExpiredItemsAsync()
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-RetentionDays);

            var expiredNodes = await _context.StorageNodes
                .Where(n => n.DeletedAt != null && n.DeletedAt <= cutoffDate)
                .ToListAsync();

            if (expiredNodes.Count == 0)
            {
                _logger.LogDebug("Lejárt elemek végleges törlése lefutott: nem volt törölhető elem. CutoffDate: {CutoffDate}", cutoffDate);
                return 0;
            }

            // Fizikai fájlok törlése a lemezről a végleges adatbázis-törlés előtt
            foreach (var node in expiredNodes.Where(n => n.Type == 2 && !string.IsNullOrEmpty(n.StoragePath)))
            {
                try
                {
                    if (File.Exists(node.StoragePath))
                    {
                        File.Delete(node.StoragePath);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Nem sikerült törölni a lejárt fizikai fájlt a lemezről. NodeId: {NodeId}, StoragePath: {StoragePath}",
                        node.Id, node.StoragePath);
                }
            }

            _context.StorageNodes.RemoveRange(expiredNodes);

            var expiredProjects = await _context.Projects
                .Where(p => p.DeletedAt != null && p.DeletedAt <= cutoffDate)
                .ToListAsync();
            _context.Projects.RemoveRange(expiredProjects);

            var expiredWorkspaces = await _context.Workspaces
                .Where(w => w.DeletedAt != null && w.DeletedAt <= cutoffDate)
                .ToListAsync();
            _context.Workspaces.RemoveRange(expiredWorkspaces);

            var deletedCount = await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Lejárt elemek végleges törlése (hard delete) befejeződött. Törölt rekordok száma: {DeletedCount}, CutoffDate: {CutoffDate}",
                deletedCount, cutoffDate);

            return deletedCount;
        }


        public async Task<IEnumerable<DeletedItemDto>> GetDeletedItemsAsync(Guid userId)
        {
            var result = new List<DeletedItemDto>();

            var deletedNodes = await _context.StorageNodes
                .AsNoTracking()
                .Where(n => n.DeletedAt != null && n.CreatedBy == userId)
                .ToListAsync();

            foreach (var node in deletedNodes)
            {
                var expiresAt = node.DeletedAt!.Value.AddDays(RetentionDays);
                result.Add(new DeletedItemDto(
                    "StorageNode",
                    node.Id.ToString(),
                    node.DeletedAt.Value,
                    expiresAt,
                    expiresAt > DateTime.UtcNow
                ));
            }

            var deletedProjects = await _context.Projects
                .AsNoTracking()
                .Where(p => p.DeletedAt != null && p.OwnerId == userId)
                .ToListAsync();

            foreach (var proj in deletedProjects)
            {
                var expiresAt = proj.DeletedAt!.Value.AddDays(RetentionDays);
                result.Add(new DeletedItemDto("Project", proj.Id.ToString(), proj.DeletedAt.Value, expiresAt, expiresAt > DateTime.UtcNow));
            }

            var deletedWorkspaces = await _context.Workspaces
                .AsNoTracking()
                .Where(w => w.DeletedAt != null && w.OwnerId == userId)
                .ToListAsync();

            foreach (var ws in deletedWorkspaces)
            {
                var expiresAt = ws.DeletedAt!.Value.AddDays(RetentionDays);
                result.Add(new DeletedItemDto("Workspace", ws.Id.ToString(), ws.DeletedAt.Value, expiresAt, expiresAt > DateTime.UtcNow));
            }

            return result;
        }
    }
}