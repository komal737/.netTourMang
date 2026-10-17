using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using MailKit.Net.Smtp;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace TourManagement_MVC.Services
{
    public interface IEmailService
    {
        Task SendBookingConfirmationAsync(string toEmail, string subject, string htmlMessage);
        Task SendBookingCancellationAsync(string toEmail, string subject, string htmlMessage);
        Task SendContactMessageAsync(string toEmail, string subject, string htmlMessage);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration config, ILogger<EmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        // Helper for reading configuration values with fallbacks to environment variables.
        private string? GetConfig(string key, string? envName = null)
        {
            var v = _config[key];
            if (!string.IsNullOrEmpty(v)) return v;
            if (!string.IsNullOrEmpty(envName)) return System.Environment.GetEnvironmentVariable(envName);
            return null;
        }

        public async Task SendBookingConfirmationAsync(string toEmail, string subject, string htmlMessage)
            => await SendAsync(toEmail, subject, htmlMessage);

        public async Task SendBookingCancellationAsync(string toEmail, string subject, string htmlMessage)
            => await SendAsync(toEmail, subject, htmlMessage);

        public async Task SendContactMessageAsync(string toEmail, string subject, string htmlMessage)
            => await SendAsync(toEmail, subject, htmlMessage);

        private async Task SendAsync(string toEmail, string subject, string htmlMessage)
        {
            // Prefer SendGrid if API key configured
            var sendGridKey = GetConfig("SendGrid:ApiKey", "SENDGRID_APIKEY"); 
            if (!string.IsNullOrEmpty(sendGridKey))
            {
                try
                {
                    var client = new SendGridClient(sendGridKey);
                    var from = new EmailAddress(_config["Email:From"] ?? "noreply@example.com", _config["Email:FromName"] ?? "Tour Management");
                    var to = new EmailAddress(toEmail);
                    var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent: null, htmlContent: htmlMessage);
                    var response = await client.SendEmailAsync(msg);
                    _logger.LogInformation("SendGrid email to {To} responded {Status}", toEmail, response.StatusCode);
                    return;
                }
                catch (System.Exception ex)
                {
                    _logger.LogWarning(ex, "SendGrid send failed, falling back to SMTP");
                }
            }

            // Fallback to SMTP via MailKit
            var smtpHost = GetConfig("Smtp:Host", "SMTP_HOST");
            var smtpPort = _config.GetValue<int?>("Smtp:Port") ?? 25;
            var smtpUser = GetConfig("Smtp:Username", "SMTP_USERNAME");
            var smtpPass = GetConfig("Smtp:Password", "SMTP_PASSWORD");

            var msg2 = new MimeMessage();
            msg2.From.Add(new MailboxAddress(_config["Email:FromName"] ?? "Tour Management", _config["Email:From"] ?? "noreply@example.com"));
            msg2.To.Add(MailboxAddress.Parse(toEmail));
            msg2.Subject = subject;
            msg2.Body = new TextPart("html") { Text = htmlMessage };

            try
            {
                using var client = new SmtpClient();
                // If host is not configured, abort early so caller can see a warning
                if (string.IsNullOrEmpty(smtpHost))
                {
                    _logger.LogWarning("SMTP host not configured; skipping SMTP send");
                    return;
                }
                await client.ConnectAsync(smtpHost, smtpPort, MailKit.Security.SecureSocketOptions.Auto);
                if (!string.IsNullOrEmpty(smtpUser))
                {
                    await client.AuthenticateAsync(smtpUser, smtpPass);
                }
                await client.SendAsync(msg2);
                await client.DisconnectAsync(true);
                _logger.LogInformation("SMTP email sent to {To}", toEmail);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {To}", toEmail);
            }
        }
    }
}
