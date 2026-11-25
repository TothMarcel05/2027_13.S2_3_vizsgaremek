using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PROTOTYPE_backend.Data;
using PROTOTYPE_backend.DTOs.Storage;
using PROTOTYPE_backend.Models;

namespace PROTOTYPE_backend.Services.Storage
{
    public class StorageService : IStorageService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<StorageService> _logger;

        public StorageService(AppDbContext context, ILogger<StorageService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<StorageNodeResponseDto> UploadFileAsync(UploadFileDto dto)
        {
            var workspace = await _context.Workspaces.FindAsync(dto.WorkspaceId);
            if (workspace == null)
            {
                _logger.LogWarning(
                    "Fájlfeltöltés elutasítva: a munkaterület nem található. WorkspaceId: {WorkspaceId}, UserId: {UserId}",
                    dto.WorkspaceId, dto.UserId);
                throw new KeyNotFoundException("A megadott munkaterület nem található.");
            }

            var currentUsedBytes = await _context.StorageNodes
                .Where(n => n.WorkspaceId == dto.WorkspaceId && n.DeletedAt == null && n.Type == 2)
                .SumAsync(n => (long)(n.SizeBytes ?? 0));

            if ((ulong)(currentUsedBytes + dto.File.Length) > workspace.StorageQuotaBytes)
            {
                _logger.LogWarning(
                    "Fájlfeltöltés elutasítva kvótatúllépés miatt. WorkspaceId: {WorkspaceId}, UserId: {UserId}, Használt: {UsedBytes}, Fájlméret: {FileSize}, Kvóta: {Quota}",
                    dto.WorkspaceId, dto.UserId, currentUsedBytes, dto.File.Length, workspace.StorageQuotaBytes);
                throw new InvalidOperationException("A feltöltés meghiúsult: a munkaterület tárhelykvótája betelt.");
            }

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
                _logger.LogInformation("Uploads könyvtár létrehozva: {UploadsFolder}", uploadsFolder);
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

                _logger.LogInformation(
                    "Fájl sikeresen feltöltve. NodeId: {NodeId}, WorkspaceId: {WorkspaceId}, ProjectId: {ProjectId}, UserId: {UserId}, Méret: {SizeBytes} bájt",
                    nodeId, dto.WorkspaceId, dto.ProjectId, dto.UserId, dto.File.Length);

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
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Fájlfeltöltési tranzakció megszakadt. WorkspaceId: {WorkspaceId}, UserId: {UserId}, Fájlnév: {FileName}",
                    dto.WorkspaceId, dto.UserId, dto.File.FileName);

                if (File.Exists(filePath))
                {
                    try
                    {
                        File.Delete(filePath);
                        _logger.LogInformation("Kompenzáció: sikertelen feltöltés utáni árva fájl törölve: {FilePath}", filePath);
                    }
                    catch (Exception deleteEx)
                    {
                        _logger.LogError(deleteEx, "Kompenzációs hiba: nem sikerült törölni az árva fájlt a lemezről: {FilePath}", filePath);
                    }
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

            _logger.LogInformation(
                "Új mappa létrehozva. NodeId: {NodeId}, WorkspaceId: {WorkspaceId}, ProjectId: {ProjectId}, UserId: {UserId}",
                folderNode.Id, workspaceId, projectId, userId);

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

            if (node == null)
            {
                _logger.LogWarning("Fájlletöltés sikertelen: a node nem található vagy törölt. NodeId: {NodeId}", nodeId);
                return null;
            }

            if (string.IsNullOrEmpty(node.StoragePath) || !File.Exists(node.StoragePath))
            {
                _logger.LogError(
                    "Adatinkonzisztencia: a fájl szerepel az adatbázisban, de hiányzik a fizikai lemezről. NodeId: {NodeId}, StoragePath: {StoragePath}",
                    nodeId, node.StoragePath);
                return null;
            }

            _logger.LogDebug("Fájl folyam megnyitva letöltéshez. NodeId: {NodeId}, Méret: {SizeBytes} bájt", nodeId, node.SizeBytes);

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
                _logger.LogWarning("Node jogosultság-megosztás sikertelen: érvénytelen node vagy role. NodeId: {NodeId}, RoleId: {RoleId}", nodeId, roleId);
                return false;
            }

            if (!node.Roles.Any(r => r.Id == roleId))
            {
                node.Roles.Add(role);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Storage node megosztva szerepkörrel. NodeId: {NodeId}, RoleId: {RoleId}", nodeId, roleId);
            }

            return true;
        }

        public async Task<bool> SoftDeleteNodeAsync(Guid nodeId, Guid userId)
        {
            var node = await _context.StorageNodes.FindAsync(nodeId);
            if (node == null || node.DeletedAt != null)
            {
                _logger.LogWarning("Soft delete sikertelen: a node nem található vagy már törölve van. NodeId: {NodeId}, UserId: {UserId}", nodeId, userId);
                return false;
            }

            node.DeletedAt = DateTime.UtcNow;
            node.UpdatedBy = userId;

            await _context.SaveChangesAsync();
            _logger.LogInformation("Storage node lomtárba helyezve. NodeId: {NodeId}, UserId: {UserId}", nodeId, userId);
            return true;
        }

        public async Task<bool> RestoreNodeAsync(Guid nodeId)
        {
            var node = await _context.StorageNodes.FindAsync(nodeId);
            if (node == null || node.DeletedAt == null)
            {
                _logger.LogWarning("Node visszaállítása sikertelen: nem található vagy nincs a lomtárban. NodeId: {NodeId}", nodeId);
                return false;
            }

            node.DeletedAt = null;
            await _context.SaveChangesAsync();
            _logger.LogInformation("Storage node visszaállítva a lomtárból. NodeId: {NodeId}", nodeId);
            return true;
        }

        public async Task<NodeDataChangeResponseDto> ChangeNodeData(GetNodesRequestDto dto)
        {
            var node = await _context.StorageNodes.FindAsync(dto.NodeId);
            if (node == null || node.DeletedAt != null)
            {
                _logger.LogWarning("Node adatmódosítás sikertelen: nem található vagy törölt. NodeId: {NodeId}", dto.NodeId);
                throw new KeyNotFoundException("A megadott csomópont nem található vagy törölt.");
            }

            if (!string.IsNullOrWhiteSpace(dto.Name))
            {
                node.Name = dto.Name.Trim();
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Storage node adatai módosítva. NodeId: {NodeId}, ÚjNév: {Name}", node.Id, node.Name);

            return new NodeDataChangeResponseDto(
                node.Id,
                node.WorkspaceId,
                node.Name,
                node.StoragePath
            );
        }
    }
}