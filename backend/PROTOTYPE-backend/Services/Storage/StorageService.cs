using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PROTOTYPE_backend.Data;
using PROTOTYPE_backend.DTOs.Storage;
using PROTOTYPE_backend.Models;

namespace PROTOTYPE_backend.Services.Storage
{
    public class StorageService : IStorageService
    {
        private readonly AppDbContext _context;

        public StorageService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<StorageNodeResponseDto> UploadFileAsync(UploadFileDto dto)
        {
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = $"{Guid.NewGuid()}_{dto.File.FileName}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await dto.File.CopyToAsync(stream);
            }

            var node = new StorageNode
            {
                WorkspaceId = dto.WorkspaceId,
                ProjectId = dto.ProjectId,
                ParentId = dto.ParentId,
                Name = dto.File.FileName,
                Type = 2, 
                MimeType = dto.File.ContentType,
                StoragePath = filePath,
                SizeBytes = (ulong)dto.File.Length,
                CreatedBy = dto.UserId,
                CreatedAt = DateTime.UtcNow
            };

            _context.StorageNodes.Add(node);

            if (dto.Labels != null && dto.Labels.Any())
            {
                foreach (var labelText in dto.Labels)
                {
                    _context.StorageNodeLabels.Add(new StorageNodeLabel
                    {
                        NodeId = node.Id,
                        Label = labelText
                    });
                }
            }

            await _context.SaveChangesAsync();

            return new StorageNodeResponseDto(
                node.Id,
                node.Name,
                node.Type,
                node.MimeType,
                node.SizeBytes,
                node.CreatedAt,
                dto.Labels ?? new List<string>()
            );
        }

        public async Task<IEnumerable<StorageNodeResponseDto>> GetNodesByWorkspaceAsync(Guid workspaceId, string? tagFilter)
        {
            var query = _context.StorageNodes
                .Where(n => n.WorkspaceId == workspaceId && n.DeletedAt == null)
                .Include(n => n.StorageNodeLabels)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(tagFilter))
            {
                query = query.Where(n => n.StorageNodeLabels.Any(l => l.Label.ToLower() == tagFilter.ToLower()));
            }

            var nodes = await query.ToListAsync();

            return nodes.Select(n => new StorageNodeResponseDto(
                n.Id,
                n.Name,
                n.Type,
                n.MimeType,
                n.SizeBytes,
                n.CreatedAt,
                n.StorageNodeLabels.Select(l => l.Label).ToList()
            ));
        }
        public async Task<bool> ShareNodeWithRoleAsync(Guid nodeId, Guid roleId)
        {
            var node = await _context.StorageNodes
                .Include(n => n.Roles)
                .FirstOrDefaultAsync(n => n.Id == nodeId && n.DeletedAt == null);

            var role = await _context.AppRoles.FindAsync(roleId);
            if (node == null || role == null) return false;

            if (!node.Roles.Any(r => r.Id == roleId))
            {
                node.Roles.Add(role);
                await _context.SaveChangesAsync();
            }

            return true;
        }

        public async Task<bool> SoftDeleteNodeAsync(Guid nodeId, System.Guid userId)
        {
            var node = await _context.StorageNodes.FindAsync(nodeId);
            if (node == null || node.DeletedAt != null) return false;

            node.DeletedAt = DateTime.UtcNow;
            node.UpdatedBy = userId;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RestoreNodeAsync(Guid nodeId)
        {
            var node = await _context.StorageNodes.FindAsync(nodeId);
            if (node == null || node.DeletedAt == null) return false;

            node.DeletedAt = null;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}