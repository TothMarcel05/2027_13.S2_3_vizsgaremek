using Microsoft.EntityFrameworkCore;
using PROTOTYPE_backend.Data;
using PROTOTYPE_backend.DTOs.Storage;
using PROTOTYPE_backend.Models;
using PROTOTYPE_backend.Services.SoftDelete;

namespace PROTOTYPE_backend.Services.Storage;

public interface IStorageService
{
    Task<StorageNodeDto> UploadFileAsync(FileUploadDto dto, Guid userId);
    Task<List<StorageNodeDto>> GetFilesByTagAsync(string tagName, Guid? workspaceId);
    Task<bool> SetTagRbacAsync(SetTagRbacDto dto);
    Task<bool> DeleteFileAsync(Guid fileId, Guid userId);
}

public class StorageService : IStorageService
{
    private readonly AppDbContext _context;
    private readonly ISoftDeleteService _softDeleteService;

    public StorageService(AppDbContext context, ISoftDeleteService softDeleteService)
    {
        _context = context;
        _softDeleteService = softDeleteService;
    }

    public async Task<StorageNodeDto> UploadFileAsync(FileUploadDto dto, Guid userId)
    {
        var node = new StorageNode
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Type = "file",
            MimeType = dto.MimeType,
            SizeBytes = dto.SizeBytes,
            WorkspaceId = dto.WorkspaceId,
            ProjectId = dto.ProjectId,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow
        };

        if (dto.Tags != null && dto.Tags.Any())
        {
            foreach (var tagName in dto.Tags)
            {
                var tag = await _context.StorageTags
                    .FirstOrDefaultAsync(t => t.Name.ToLower() == tagName.ToLower() && t.WorkspaceId == dto.WorkspaceId)
                    ?? new StorageTag
                    {
                        Id = Guid.NewGuid(),
                        Name = tagName,
                        WorkspaceId = dto.WorkspaceId
                    };

                node.Tags.Add(tag);
            }
        }

        _context.StorageNodes.Add(node);
        await _context.SaveChangesAsync();

        var tagNames = node.Tags.Select(t => t.Name).ToList();
        return new StorageNodeDto(node.Id, node.Name, node.Type, node.MimeType, node.SizeBytes ?? 0, tagNames, node.CreatedAt);
    }

    public async Task<List<StorageNodeDto>> GetFilesByTagAsync(string tagName, Guid? workspaceId)
    {
        var query = _context.StorageNodes
            .AsNoTracking()
            .Include(n => n.Tags)
            .Where(n => n.Tags.Any(t => t.Name.ToLower() == tagName.ToLower()));

        if (workspaceId.HasValue)
        {
            query = query.Where(n => n.WorkspaceId == workspaceId.Value);
        }

        var files = await query.ToListAsync();

        return files.Select(f => new StorageNodeDto(
            f.Id,
            f.Name,
            f.Type,
            f.MimeType,
            f.SizeBytes ?? 0,
            f.Tags.Select(t => t.Name).ToList(),
            f.CreatedAt
        )).ToList();
    }

    public async Task<bool> SetTagRbacAsync(SetTagRbacDto dto)
    {
        var existingRbac = await _context.StorageTagRbacs
            .FirstOrDefaultAsync(r => r.TagId == dto.TagId && r.RoleId == dto.RoleId);

        if (existingRbac != null)
        {
            existingRbac.CanRead = dto.CanRead;
            existingRbac.CanWrite = dto.CanWrite;
            existingRbac.CanDelete = dto.CanDelete;
        }
        else
        {
            var newRbac = new StorageTagRbac
            {
                TagId = dto.TagId,
                RoleId = dto.RoleId,
                CanRead = dto.CanRead,
                CanWrite = dto.CanWrite,
                CanDelete = dto.CanDelete
            };
            _context.StorageTagRbacs.Add(newRbac);
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteFileAsync(Guid fileId, Guid userId)
    {
        // Az 1. lépésben elkészített Soft-Delete szolgáltatást hívja meg
        return await _softDeleteService.SoftDeleteAsync<StorageNode>(fileId, userId);
    }
}