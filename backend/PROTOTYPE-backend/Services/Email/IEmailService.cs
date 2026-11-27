using static Org.BouncyCastle.Crypto.Engines.SM2Engine;

namespace PROTOTYPE_backend.Services.Email
{
    public interface IEmailService
    {
        Task SendPasswordResetEmailAsync(string toEmail, string resetToken);
        Task SendTwoFactorCodeEmailAsync(string email, string code);
    }
}
