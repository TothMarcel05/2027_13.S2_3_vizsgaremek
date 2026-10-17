namespace PROTOTYPE_backend.DTOs.Admin;

public record UserAdminDto(
    Guid Id,
    string Name,
    string Email,
    string GlobalRole,
    DateTime CreatedAt
);

public record UpdateGlobalRoleDto(
    Guid UserId,
    string NewGlobalRole
);

public record DeletedItemDto(
    Guid Id,
    string TitleOrName,
    string EntityType,
    DateTime? DeletedAt,
    Guid? DeletedBy
);

public record RestoreItemDto(
    Guid Id,
    string EntityType
);