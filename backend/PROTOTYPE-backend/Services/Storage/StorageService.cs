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

        // 1. FÁJL FELTÖLTÉSE (KVÓTA ELLENŐRZÉSSEL ÉS ROLBACK PROTKOLLAL)
        public async Task<StorageNodeResponseDto> UploadFileAsync(UploadFileDto dto)
        {
            // A) Munkaterület tárhelykvótájának ellenőrzése
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

            // B) Fájl kiírása a fizikai lemezre
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

                // C) StorageNode rekord mentése az adatbázisba (Explicit GUID és DeletedAt = null)
                var nodeId = Guid.NewGuid();
                var node = new StorageNode
                {
                    Id = nodeId,
                    WorkspaceId = dto.WorkspaceId,
                    ProjectId = dto.ProjectId,
                    ParentId = dto.ParentId,
                    Name = dto.File.FileName,
                    Type = 2, // 2 = file (storage_type)
                    MimeType = dto.File.ContentType,
                    StoragePath = filePath,
                    SizeBytes = (ulong)dto.File.Length,
                    CreatedBy = dto.UserId,
                    CreatedAt = DateTime.UtcNow,
                    DeletedAt = null // Felülírjuk az SQL DEFAULT CURRENT_TIMESTAMP értékét!
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
                // Rollback: Ha az adatbázis mentés elhasal, töröljük az árva fájlt a lemezről
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
                throw;
            }
        }

        // 2. MAPPA LÉTREHOZÁSA (STORAGE TYPE = 1)
        public async Task<StorageNodeResponseDto> CreateFolderAsync(Guid workspaceId, Guid? projectId, Guid? parentId, string folderName, Guid userId)
        {
            var folderNode = new StorageNode
            {
                Id = Guid.NewGuid(),
                WorkspaceId = workspaceId,
                ProjectId = projectId,
                ParentId = parentId,
                Name = folderName,
                Type = 1, // 1 = folder (storage_type)
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

        // 3. FÁJL LETÖLTÉSE / STREAMING
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

        // 4. NODEOK LEKÉRDEZÉSE CÍMKE SZŰRÉSSEL
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
        // 5. NODE MEGOSZTÁSA SZEREPKÖRREL (Direct Many-to-Many via EF Core Navigation)
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

            // Ha a szerepkör még nincs hozzárendelve a csomóponthoz, hozzáadjuk a navigációs gyűjteményhez
            if (!node.Roles.Any(r => r.Id == roleId))
            {
                node.Roles.Add(role);
                await _context.SaveChangesAsync();
            }

            return true;
        }

        // 6. SOFT DELETE ÉS VISSZAÁLLÍTÁS
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
    }
}