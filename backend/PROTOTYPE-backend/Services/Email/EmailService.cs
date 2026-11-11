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

        public async Task SendPasswordResetEmailAsync(string toEmail, string resetToken) 
        {
            var resetLink = $"https://sajatalkalmazas.hu/reset-password?token={Uri.EscapeDataString(resetToken)}&email={Uri.EscapeDataString(toEmail)}";

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Noreply", "noreply@okligon.hu"));
            message.To.Add(new MailboxAddress("", toEmail));
            message.Subject = "Jelszó visszaállítási kérelem";

            var builder = new BodyBuilder();
            builder.HtmlBody =
                $"<div>" +
                $"<h1>Jelszó visszaállítás!</h1>" +
                $"<p>Az alábbi linken a jelszódat módosíthatod:</p>" +
                $"<p><a href='{resetLink}'>LINK</a>" +
                $"</div>";

            message.Body = builder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(_config["Smtp:Host"], int.Parse(_config["Smtp:Port"]), SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(_config["Smtp:User"], _config["Smtp:Pass"]);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }
}
