using System.ComponentModel.DataAnnotations;

namespace PROTOTYPE_backend.DTOs.Auth
{
    public record RefreshTokenRequestDto 
        (
        [Required] string RefreshToken
    );
}
