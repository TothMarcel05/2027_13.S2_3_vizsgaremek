using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PROTOTYPE_backend.Data;
using PROTOTYPE_backend.DTOs.SoftDelete;
using PROTOTYPE_backend.Models;

namespace PROTOTYPE_backend.Services.SoftDelete
{
    public class SoftDeleteService : ISoftDeleteService
    {
        private readonly AppDbContext _context;
        private const int RetentionDays = 30; 

        public SoftDeleteService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<bool> SoftDeleteAsync(SoftDeleteRequestDto dto)
        {
            if (dto.EntityName == "StorageNode")
            {
                if (!Guid.TryParse(dto.EntityId, out var nodeId)) return false;
                var node = await _context.StorageNodes.FindAsync(nodeId);
                if (node == null || node.DeletedAt != null) return false;

                node.DeletedAt = DateTime.UtcNow;
                node.UpdatedBy = dto.UserId;
            }
            else if (dto.EntityName == "AppUser")
            {
                var user = await _context.AppUsers.FindAsync(dto.EntityId);
                if (user == null || user.DeletedAt != null) return false;

                user.DeletedAt = DateTime.UtcNow;
            }
            else
            {
                return false;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RestoreAsync(RestoreRequestDto dto)
        {
            if (dto.EntityName == "StorageNode")
            {
                if (!Guid.TryParse(dto.EntityId, out var nodeId)) return false;
                var node = await _context.StorageNodes.FindAsync(nodeId);
                if (node == null || node.DeletedAt == null) return false;

                // Ellenőrzés: lejárt-e a megőrzési idő
                if (node.DeletedAt.Value.AddDays(RetentionDays) < DateTime.UtcNow) return false;

                node.DeletedAt = null;
            }
            else if (dto.EntityName == "AppUser")
            {
                var user = await _context.AppUsers.FindAsync(dto.EntityId);
                if (user == null || user.DeletedAt == null) return false;

                if (user.DeletedAt.Value.AddDays(RetentionDays) < DateTime.UtcNow) return false;

                user.DeletedAt = null;
            }
            else
            {
                return false;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<DeletedItemDto>> GetDeletedItemsAsync(string userId)
        {
            var result = new List<DeletedItemDto>();

            var deletedNodes = await _context.StorageNodes
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

            return result;
        }

        public async Task<int> HardDeleteExpiredItemsAsync()
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-RetentionDays);

            var expiredNodes = await _context.StorageNodes
                .Where(n => n.DeletedAt != null && n.DeletedAt <= cutoffDate)
                .ToListAsync();

            _context.StorageNodes.RemoveRange(expiredNodes);

            return await _context.SaveChangesAsync();
        }
    }
}