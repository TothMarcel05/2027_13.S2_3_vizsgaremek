namespace PROTOTYPE_backend.DTOs.Storage;

public record FileUploadDto(
    string Name,
    string MimeType,
    long SizeBytes,
    Guid? WorkspaceId,
    Guid? ProjectId,
    List<string>? Tags
);

public record StorageNodeDto(
    Guid Id,
    string Name,
    string Type,
    string? MimeType,
    long SizeBytes,
    List<string> Tags,
    DateTime CreatedAt
);

public record SetTagRbacDto(
    Guid TagId,
    Guid RoleId,
    bool CanRead,
    bool CanWrite,
    bool CanDelete
);