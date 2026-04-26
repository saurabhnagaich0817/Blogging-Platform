using InkWell.Shared.Events;
using MassTransit;
using InkWell.Shared.Services;

namespace InkWell.NotificationService.Consumers
{
    public class UserLoggedInConsumer : IConsumer<UserLoggedInEvent>
    {
        private readonly ILogger<UserLoggedInConsumer> _logger;
        private readonly IEmailService _emailService;

        public UserLoggedInConsumer(ILogger<UserLoggedInConsumer> logger, IEmailService emailService)
        {
            _logger = logger;
            _emailService = emailService;
        }

        public async Task Consume(ConsumeContext<UserLoggedInEvent> context)
        {
            var message = context.Message;
            _logger.LogInformation($"[LOGIN EVENT] User logged in: {message.Email}");

            // 1. Send Login Notification Email to User (Security Alert)
            var subject = "Security Alert: New Login Detected";
            var body = $@"
                <div style='font-family: Arial, sans-serif; padding: 20px; color: #333;'>
                    <h2 style='color: #E67E22;'>New Login Detected</h2>
                    <p>Hi <strong>{message.FullName}</strong>,</p>
                    <p>Your InkWell account was just logged into at <strong>{message.LoginTime:f}</strong>.</p>
                    <p>If this was you, you can safely ignore this email. If not, please change your password immediately.</p>
                    <br/>
                    <p>Best regards,<br/>The InkWell Team</p>
                </div>";

            await _emailService.SendEmailAsync(message.Email, subject, body);
        }
    }
}
