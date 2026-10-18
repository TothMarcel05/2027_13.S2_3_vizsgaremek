namespace PROTOTYPE_backend.DTOs.Auth
{
    public record TwoFactorLoginDto(string TwoFactorToken, string Code);
    public record TwoFactorSetupDto(string Secret, string OtpAuthUri);
    public record TwoFactorCodeDto(string Code);
}
