using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace PROTOTYPE_backend.Services.Email
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
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
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Noreply", "noreply@okligon.hu"));
            message.To.Add(new MailboxAddress("", toEmail));
            message.Subject = subject;

            var builder = new BodyBuilder
            {
                HtmlBody = htmlBody
            };
            message.Body = builder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(_config["Smtp:Host"], int.Parse(_config["Smtp:Port"]!), SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(_config["Smtp:User"], _config["Smtp:Pass"]);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }
}