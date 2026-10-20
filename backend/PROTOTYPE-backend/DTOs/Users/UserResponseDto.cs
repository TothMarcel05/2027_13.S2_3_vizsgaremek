namespace PROTOTYPE_backend.DTOs.Users
{
    public record UserResponseDto
        (
            string Name,
            string Email,
            DateTime UpdatedAt
        );
}
