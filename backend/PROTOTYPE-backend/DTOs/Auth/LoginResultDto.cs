namespace PROTOTYPE_backend.DTOs.Auth
{
    public record LoginResultDto
    (
        bool RequiresTwoFactor,
        int? TwoFactorType,
        string? TwoFactorToken, 
        AuthResponseDto? Auth);
}
