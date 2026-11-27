using System.ComponentModel.DataAnnotations;

namespace PROTOTYPE_backend.DTOs.Users
{
    // 1. Saját profil részletes lekerdezese
    public record UserResponseDto(
        Guid UserId,
        string Name,
        string Email,
        string? AvatarUrl,
        DateTime CreatedAt,
        DateTime? UpdatedAt,
        sbyte GlobalRole,
        bool Is2faEnabled,
        sbyte TwoFactorType
    );

    // 2. Profil modositas kérelem
    public record UserModifyRequestDto(
        [Required]
        [StringLength(100, MinimumLength = 2)]
        string Name,

        [Url]
        string? AvatarURL
    );

    // 3. Profil modositas valasz
    public record UserModfiyResponseDto(
        Guid UserId,
        string Name,
        string Email,
        string? AvatarUrl,
        DateTime UpdatedAt
    );

    // 4. Soft delete valasz
    public record UserDeleteResponseDto(
        Guid UserId,
        DateTime DeletedAt
    );

    // 5. Jelszováltoztatas
    public record ChangePasswordRequestDto(
        [Required]
        string CurrentPassword,

        [Required]
        [StringLength(100, MinimumLength = 8)]
        string NewPassword
    );

    // 6. Csapattagok altal lathato nyilvanos profil (pl. feladatokhoz, naptarhoz)
    public record UserPublicDto(
        Guid UserId,
        string Name,
        string Email,
        string? AvatarUrl
    );
}