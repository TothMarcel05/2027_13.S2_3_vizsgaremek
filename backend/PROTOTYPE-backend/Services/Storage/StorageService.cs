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
            var workspace = await _context.Workspaces.FindAsync(dto.WorkspaceId);
            if (workspace == null)
            {
                throw new ArgumentException("A megadott munkaterület nem található.");
            }

            var currentUsedBytes = await _context.StorageNodes
                .Where(n => n.WorkspaceId == dto.WorkspaceId && n.DeletedAt == null && n.Type == 2)
                .SumAsync(n => (long)(n.SizeBytes ?? 0));

            if ((ulong)(currentUsedBytes + dto.File.Length) > workspace.StorageQuotaBytes)
            {
                throw new InvalidOperationException("A feltöltés meghiúsult: a munkaterület tárhelykvótája betelt.");
            }

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueFileName = $"{Guid.NewGuid()}_{dto.File.FileName}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            try
            {
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await dto.File.CopyToAsync(stream);
                }

                var nodeId = Guid.NewGuid();
                var node = new StorageNode
                {
                    Id = nodeId,
                    WorkspaceId = dto.WorkspaceId,
                    ProjectId = dto.ProjectId,
                    ParentId = dto.ParentId,
                    Name = dto.File.FileName,
                    Type = 2, 
                    MimeType = dto.File.ContentType,
                    StoragePath = filePath,
                    SizeBytes = (ulong)dto.File.Length,
                    CreatedBy = dto.UserId,
                    CreatedAt = DateTime.UtcNow,
                    DeletedAt = null 
                };

                _context.StorageNodes.Add(node);

                if (dto.Labels != null && dto.Labels.Any())
                {
                    foreach (var labelText in dto.Labels)
                    {
                        _context.StorageNodeLabels.Add(new StorageNodeLabel
                        {
                            Id = Guid.NewGuid(),
                            NodeId = nodeId,
                            Label = labelText,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }

                await _context.SaveChangesAsync();

                return new StorageNodeResponseDto(
                    node.Id,
                    node.WorkspaceId,
                    node.Name,
                    node.Type,
                    node.MimeType,
                    node.SizeBytes,
                    node.CreatedAt,
                    dto.Labels ?? new List<string>()
                );
            }
            catch
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
                throw;
            }
        }

        public async Task<StorageNodeResponseDto> CreateFolderAsync(Guid workspaceId, Guid? projectId, Guid? parentId, string folderName, Guid userId)
        {
            var folderNode = new StorageNode
            {
                Id = Guid.NewGuid(),
                WorkspaceId = workspaceId,
                ProjectId = projectId,
                ParentId = parentId,
                Name = folderName,
                Type = 1, 
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow,
                DeletedAt = null
            };

            _context.StorageNodes.Add(folderNode);
            await _context.SaveChangesAsync();

            return new StorageNodeResponseDto(
                folderNode.Id,
                folderNode.WorkspaceId,
                folderNode.Name,
                folderNode.Type,
                null,
                0,
                folderNode.CreatedAt,
                new List<string>()
            );
        }

        public async Task<(Stream FileStream, string ContentType, string FileName)?> GetFileStreamAsync(Guid nodeId)
        {
            var node = await _context.StorageNodes
                .AsNoTracking()
                .FirstOrDefaultAsync(n => n.Id == nodeId && n.Type == 2 && n.DeletedAt == null);

            if (node == null || string.IsNullOrEmpty(node.StoragePath) || !File.Exists(node.StoragePath))
            {
                return null;
            }

            var memoryStream = new FileStream(node.StoragePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var contentType = node.MimeType ?? "application/octet-stream";

            return (memoryStream, contentType, node.Name);
        }

        public async Task<IEnumerable<StorageNodeResponseDto>> GetNodesByWorkspaceAsync(Guid workspaceId, string? tagFilter)
        {
            var query = _context.StorageNodes
                .AsNoTracking()
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
                n.WorkspaceId,
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

            if (node == null || role == null)
            {
                return false;
            }


            if (!node.Roles.Any(r => r.Id == roleId))
            {
                node.Roles.Add(role);
                await _context.SaveChangesAsync();
            }

            return true;
        }

        public async Task<bool> SoftDeleteNodeAsync(Guid nodeId, Guid userId)
        {
            var node = await _context.StorageNodes.FindAsync(nodeId);
            if (node == null || node.DeletedAt != null)
            {
                return false;
            }

            node.DeletedAt = DateTime.UtcNow;
            node.UpdatedBy = userId;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RestoreNodeAsync(Guid nodeId)
        {
            var node = await _context.StorageNodes.FindAsync(nodeId);
            if (node == null || node.DeletedAt == null)
            {
                return false;
            }

            node.DeletedAt = null;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<NodeDataChangeResponseDto> ChangeNodeData(GetNodesRequestDto dto)
        {
            var node = await _context.StorageNodes.FindAsync(dto.NodeId);
            if (node == null || node.DeletedAt != null)
            {
                throw new ArgumentException("A megadott csomópont nem található vagy törölt.");
            }

            await _context.SaveChangesAsync();
            return new NodeDataChangeResponseDto(
                node.Id,
                node.WorkspaceId,
                node.Name,
                node.StoragePath
            );
        }
    }
}