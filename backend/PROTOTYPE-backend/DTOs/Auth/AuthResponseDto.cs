// KM

namespace PROTOTYPE_backend.DTOs.Auth
{
    public record AuthResponseDto(
        string AccessToken,
        string RefreshToken,
        UserDto User
        );
}
