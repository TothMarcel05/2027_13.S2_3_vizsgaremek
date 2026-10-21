namespace PROTOTYPE_backend.DTOs.Users
{
    public record UserModfiyResponseDto
        (
            Guid UserId,
            string Name,
            string Email,
            DateTime UpdatedAt
        );
}
