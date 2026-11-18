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