using System.Diagnostics;
using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace PROTOTYPE_backend.Services.Email
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration config, ILogger<EmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task SendPasswordResetEmailAsync(string toEmail, string resetLink)
        {
            var htmlBody =
                $"<div>" +
                $"<h1>Jelszó visszaállítás!</h1>" +
                $"<p>Az alábbi linken a jelszódat módosíthatod:</p>" +
                $"<p><a href='{resetLink}'>LINK</a></p>" +
                $"</div>";

            await SendEmailAsync(toEmail, "Jelszó visszaállítási kérelem", htmlBody);
        }

        public async Task SendTwoFactorCodeEmailAsync(string toEmail, string code)
        {
            var htmlBody =
                $"<div style='font-family: Arial, sans-serif; max-width: 500px;'>" +
                $"<h2>Kétfaktoros hitelesítési kód</h2>" +
                $"<p>A bejelentkezéshez szükséges egyszeri biztonsági kódod:</p>" +
                $"<div style='font-size: 28px; font-weight: bold; letter-spacing: 6px; " +
                $"padding: 12px 20px; background-color: #f4f4f5; border-radius: 6px; " +
                $"display: inline-block; margin: 10px 0;'>{code}</div>" +
                $"<p style='color: #666; font-size: 13px;'>A kód 5 percig érvényes. Ha nem te próbáltál meg belépni, hagyd figyelmen kívül ezt az üzenetet.</p>" +
                $"</div>";

            await SendEmailAsync(toEmail, "Bejelentkezési biztonsági kód", htmlBody);
        }

        private async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            var host = _config["Smtp:Host"];
            var portStr = _config["Smtp:Port"];
            var user = _config["Smtp:User"];
            var pass = _config["Smtp:Pass"];

            if (string.IsNullOrWhiteSpace(host) || !int.TryParse(portStr, out var port))
            {
                _logger.LogCritical("Hiányzó vagy érvénytelen SMTP konfiguráció (Smtp:Host vagy Smtp:Port)!");
                throw new InvalidOperationException("Az SMTP szerver nincs megfelelően konfigurálva.");
            }

            var maskedEmail = MaskEmail(toEmail);

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Noreply", "noreply@okligon.hu"));
            message.To.Add(new MailboxAddress("", toEmail));
            message.Subject = subject;

            var builder = new BodyBuilder
            {
                HtmlBody = htmlBody
            };
            message.Body = builder.ToMessageBody();

            var stopwatch = Stopwatch.StartNew();
            using var client = new SmtpClient();

            try
            {
                _logger.LogDebug("SMTP kapcsolat indítása. Host: {SmtpHost}, Port: {SmtpPort}, Tárgy: {Subject}, Címzett: {MaskedEmail}",
                    host, port, subject, maskedEmail);

                await client.ConnectAsync(host, port, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(user, pass);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                stopwatch.Stop();
                _logger.LogInformation(
                    "E-mail sikeresen elküldve. Tárgy: {Subject}, Címzett: {MaskedEmail}, Időtartam: {ElapsedMs} ms",
                    subject, maskedEmail, stopwatch.ElapsedMilliseconds);
            }
            catch (AuthenticationException authEx)
            {
                stopwatch.Stop();
                _logger.LogCritical(
                    authEx,
                    "SMTP hitelesítési hiba! Ellenőrizd az Smtp:User és Smtp:Pass beállításokat. Host: {SmtpHost}, Tárgy: {Subject}",
                    host, subject);
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(
                    ex,
                    "E-mail küldése meghiúsult. Host: {SmtpHost}, Tárgy: {Subject}, Címzett: {MaskedEmail}, Időtartam: {ElapsedMs} ms",
                    host, subject, maskedEmail, stopwatch.ElapsedMilliseconds);
                throw;
            }
        }

        // GDPR-barát maszkolás: pl. "janos.kovacs@pelda.hu" -> "ja***@pelda.hu"
        private static string MaskEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
                return "***";

            var parts = email.Split('@');
            var local = parts[0];
            var domain = parts[1];

            var visiblePrefix = local.Length <= 2 ? local[..1] : local[..2];
            return $"{visiblePrefix}***@{domain}";
        }
    }
}