namespace PROTOTYPE_backend.DTOs.Users
{
    public record UserResponseDto
        (
            Guid UserId,
            string Name,
            string Email,
            DateTime UpdatedAt
        );
}
