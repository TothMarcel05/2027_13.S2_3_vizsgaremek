namespace PROTOTYPE_backend.DTOs.Users
{
    public record UserDeleteResponseDto
        (
            Guid UserId,
            DateTime DeletedAt
        );
}
