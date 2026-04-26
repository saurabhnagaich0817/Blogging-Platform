using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using Microsoft.Extensions.Logging;

namespace InkWell.Shared.Services
{
    public class EmailService : IEmailService
    {
        private readonly MailSettings _mailSettings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<MailSettings> mailSettings, ILogger<EmailService> logger)
        {
            _mailSettings = mailSettings.Value;
            _logger = logger;
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            try
            {
                var email = new MimeMessage();
                email.Sender = MailboxAddress.Parse(_mailSettings.SenderEmail);
                email.From.Add(new MailboxAddress(_mailSettings.SenderName, _mailSettings.SenderEmail));
                email.To.Add(MailboxAddress.Parse(to));
                email.Subject = subject;

                var builder = new BodyBuilder();
                builder.HtmlBody = body;
                email.Body = builder.ToMessageBody();

                using var smtp = new SmtpClient();
                _logger.LogInformation("Connecting to SMTP server {Host}:{Port}", _mailSettings.Host, _mailSettings.Port);
                
                await smtp.ConnectAsync(_mailSettings.Host, _mailSettings.Port, SecureSocketOptions.StartTls);
                
                _logger.LogInformation("Authenticating with SMTP server as {SenderEmail}", _mailSettings.SenderEmail);
                await smtp.AuthenticateAsync(_mailSettings.SenderEmail, _mailSettings.Password);
                
                _logger.LogInformation("Sending email to {To}", to);
                await smtp.SendAsync(email);
                
                await smtp.DisconnectAsync(true);
                _logger.LogInformation("Email sent successfully to {To}", to);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email to {To}", to);
                // We don't throw here to avoid failing the whole background task if email fails
            }
        }
    }
}
