using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

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
            // 🚀 FIRE AND FORGET: Send in background so UI doesn't hang
            _ = Task.Run(async () =>
            {
                try
                {
                    var email = new MimeMessage();
                    email.Sender = MailboxAddress.Parse(_mailSettings.SenderEmail);
                    email.From.Add(new MailboxAddress(_mailSettings.SenderName, _mailSettings.SenderEmail));
                    email.To.Add(MailboxAddress.Parse(to));
                    email.Subject = subject;

                    var builder = new BodyBuilder { HtmlBody = body };
                    email.Body = builder.ToMessageBody();

                    using var smtp = new SmtpClient();
                    
                    _logger.LogInformation("Attempting background email to {To} via {Host}:{Port}", to, _mailSettings.Host, _mailSettings.Port);

                    // 🛠️ RELAXED CONNECTION: Use Auto or StartTls based on port
                    var options = _mailSettings.Port == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.Auto;
                    
                    // Add a small timeout so we don't wait forever in background
                    smtp.Timeout = 15000; // 15 seconds

                    await smtp.ConnectAsync(_mailSettings.Host, _mailSettings.Port, options);
                    await smtp.AuthenticateAsync(_mailSettings.SenderEmail, _mailSettings.Password);
                    await smtp.SendAsync(email);
                    await smtp.DisconnectAsync(true);

                    _logger.LogInformation("✅ Email successfully sent to {To}", to);
                }
                catch (Exception ex)
                {
                    _logger.LogError("❌ Background Email Failure to {To}: {Message}", to, ex.Message);
                }
            });

            // Return immediately
            await Task.CompletedTask;
        }
    }
}
