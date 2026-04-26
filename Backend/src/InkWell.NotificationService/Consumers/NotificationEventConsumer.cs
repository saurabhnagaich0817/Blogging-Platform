using InkWell.NotificationService.Data;
using InkWell.NotificationService.Models;
using InkWell.Shared.Events;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using InkWell.Shared.Services;

namespace InkWell.NotificationService.Consumers
{
    public class NotificationEventConsumer : IConsumer<NotificationEvent>
    {
        private readonly NotificationDbContext _context;
        private readonly ILogger<NotificationEventConsumer> _logger;
        private readonly IEmailService _emailService;

        public NotificationEventConsumer(NotificationDbContext context, ILogger<NotificationEventConsumer> logger, IEmailService emailService)
        {
            _context = context;
            _logger = logger;
            _emailService = emailService;
        }

        public async Task Consume(ConsumeContext<NotificationEvent> context)
        {
            var msg = context.Message;
            Console.WriteLine($"[RECEIVED] Notification message: {msg.Title} for User: {msg.UserId}");
            _logger.LogInformation("Processing notification event for User: {UserId}", msg.UserId);

            // 1. Save to Database
            var notification = new Notification
            {
                NotificationId = Guid.NewGuid(),
                UserId = msg.UserId,
                Title = msg.Title,
                Message = msg.Message,
                Type = msg.Type,
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                RelatedId = msg.ReferenceId
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            // 2. Send Email if Email is provided
            if (!string.IsNullOrEmpty(msg.UserEmail))
            {
                _logger.LogInformation("Sending email notification to {Email}", msg.UserEmail);
                var body = $@"
                    <div style='font-family: Arial, sans-serif; padding: 20px; color: #333;'>
                        <h2 style='color: #4A90E2;'>{msg.Title}</h2>
                        <p>{msg.Message}</p>
                        <br/>
                        <p>Best regards,<br/>The InkWell Team</p>
                    </div>";
                
                await _emailService.SendEmailAsync(msg.UserEmail, msg.Title, body);
            }
        }
    }
}
