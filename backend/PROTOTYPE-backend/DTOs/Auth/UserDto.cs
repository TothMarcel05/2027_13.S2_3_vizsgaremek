namespace PROTOTYPE_backend.DTOs.Auth
{
    public record UserDto
    (
        Guid Id,
        string Username,
        string Email,
        DateTime CreatedAt
    );
}
