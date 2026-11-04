using System.ComponentModel.DataAnnotations;

namespace PROTOTYPE_backend.DTOs.Auth
{
    public record AuthResponseDto(
        string AccessToken,
        string RefreshToken,
        UserDto User
        );

    public record LoginDto
    (
        [Required]
        [EmailAddress(ErrorMessage = "Az email cím formátuma nem megfelelő!")]
        string Email,
        [Required(ErrorMessage ="A jelszó megadása kötelező!")]
        string Password
    );

    public record LoginResultDto
    (
        bool RequiresTwoFactor,
        int? TwoFactorType,
        string? TwoFactorToken,
        AuthResponseDto? Auth
    );

    public record PermissionResultDtos
    (
        Dictionary<sbyte, List<string>> Permissions
    );

    public record RefreshTokenRequestDto
        (
        [Required] string RefreshToken
    );

    public record RegisterDto
    (
        [Required]
        [StringLength(30, MinimumLength =5)]
        string Username,
        [Required]
        [StringLength (30, MinimumLength = 5)]
        string Password,
        [Required]
        [StringLength(100,  MinimumLength = 5)]
        [EmailAddress(ErrorMessage = "Az email cím formátum nem megfelelő!")]
        string Email
    );

    public record TwoFactorLoginDto(string TwoFactorToken, string Code);
    public record TwoFactorSetupDto(string Secret, string OtpAuthUri);
    public record TwoFactorCodeDto(string Code);

    public record UserDto
    (
        Guid Id,
        string Username,
        string Email,
        DateTime CreatedAt
    );

    public record WorkspaceTokenDto(string WorkspaceToken);

    public record RolesResultDto(List<string> Roles);

    public record SpecificRoleResultDto
        (
            Guid Id,
            string Name,
            string Description,
            bool isSystem,
            DateTime? CreatedAt
        );

    public record CreateRoleDto
        (
            Guid Id,
            string Name,
            string Description,
            bool isSystem
        );

    public record CreateRoleResultDto
        (
            Guid Id,
            DateTime? CreatedAt
        );

    public record ModifyRoleDto
        (
            string Name,
            string Description
        );

    public record ModifyRoleResultDto
        (
            Guid Id,
            string Name,
            string Description
        );

    public record AddPermToRoletDto
        (
            Dictionary<sbyte, string> idNCodes
        );

    public record AddPermToRoleResultDto
        (
            List<string> codes
        );
}
