using InkWell.Shared.Events;
using MassTransit;
using InkWell.Shared.Services;
using Microsoft.Extensions.Options;

namespace InkWell.NotificationService.Consumers
{
    public class UserRegisteredConsumer : IConsumer<UserRegisteredEvent>
    {
        private readonly ILogger<UserRegisteredConsumer> _logger;
        private readonly IEmailService _emailService;
        private readonly MailSettings _mailSettings;

        public UserRegisteredConsumer(ILogger<UserRegisteredConsumer> logger, IEmailService emailService, IOptions<MailSettings> mailSettings)
        {
            _logger = logger;
            _emailService = emailService;
            _mailSettings = mailSettings.Value;
        }

        public async Task Consume(ConsumeContext<UserRegisteredEvent> context)
        {
            var message = context.Message;
            
            // 1. Send Welcome Email to User
            _logger.LogInformation($"[EMAIL EVENT] Sending Welcome Email to: {message.Email}");
            var userSubject = $"Welcome to InkWell, {message.FullName}!";
            var userBody = $@"
                <div style='font-family: Arial, sans-serif; padding: 20px; color: #333;'>
                    <h2 style='color: #4A90E2;'>Welcome to InkWell!</h2>
                    <p>Hi <strong>{message.FullName}</strong>,</p>
                    <p>We are thrilled to have you join our community as a <strong>{message.Role}</strong>.</p>
                    <p>InkWell is a place where stories matter. Start exploring or writing your first post today!</p>
                    <br/>
                    <p>Best regards,<br/>The InkWell Team</p>
                </div>";
            await _emailService.SendEmailAsync(message.Email, userSubject, userBody);

            // 2. Send Notification Email to Admin
            if (!string.IsNullOrEmpty(_mailSettings.AdminEmail))
            {
                _logger.LogInformation($"[EMAIL EVENT] Sending Admin Notification to: {_mailSettings.AdminEmail}");
                var adminSubject = "New User Registration Alert";
                var adminBody = $@"
                    <div style='font-family: Arial, sans-serif; padding: 20px; border: 1px solid #ddd;'>
                        <h3 style='color: #E67E22;'>New User Registered!</h3>
                        <p>A new user has just registered on the platform:</p>
                        <ul>
                            <li><strong>Name:</strong> {message.FullName}</li>
                            <li><strong>Email:</strong> {message.Email}</li>
                            <li><strong>Role:</strong> {message.Role}</li>
                            <li><strong>Date:</strong> {DateTime.UtcNow:f}</li>
                        </ul>
                        <p>Please review the user in the admin dashboard if necessary.</p>
                    </div>";
                await _emailService.SendEmailAsync(_mailSettings.AdminEmail, adminSubject, adminBody);
            }
        }
    }
}
