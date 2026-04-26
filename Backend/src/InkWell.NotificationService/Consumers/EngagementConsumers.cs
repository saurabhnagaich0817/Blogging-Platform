using InkWell.NotificationService.Data;
using InkWell.NotificationService.Models;
using InkWell.Shared.Events;
using MassTransit;
using InkWell.Shared.Services;

namespace InkWell.NotificationService.Consumers
{
    public class PostLikedConsumer : IConsumer<PostLikedEvent>
    {
        private readonly ILogger<PostLikedConsumer> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public PostLikedConsumer(ILogger<PostLikedConsumer> logger, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        public async Task Consume(ConsumeContext<PostLikedEvent> context)
        {
            var message = context.Message;
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();

            var notification = new Notification
            {
                NotificationId = Guid.NewGuid(),
                UserId = message.PostAuthorId,
                Message = $"{message.LikerName} liked your post!",
                Type = "Like",
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                RelatedId = message.PostId.ToString()
            };

            dbContext.Notifications.Add(notification);
            await dbContext.SaveChangesAsync();
            _logger.LogInformation($"[NOTIFICATION SAVED] Like notification for User {message.PostAuthorId}");

            // Send Email if Author Email is available
            if (!string.IsNullOrEmpty(message.PostAuthorEmail))
            {
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
                var subject = "Your post was liked!";
                var body = $@"
                    <div style='font-family: Arial, sans-serif; padding: 20px; color: #333;'>
                        <h2 style='color: #4A90E2;'>Great news!</h2>
                        <p><strong>{message.LikerName}</strong> liked your post on InkWell.</p>
                        <br/>
                        <p>Keep writing great content!</p>
                        <br/>
                        <p>Best regards,<br/>The InkWell Team</p>
                    </div>";
                await emailService.SendEmailAsync(message.PostAuthorEmail, subject, body);
            }
        }
    }

    public class UserSubscribedConsumer : IConsumer<UserSubscribedEvent>
    {
        private readonly ILogger<UserSubscribedConsumer> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public UserSubscribedConsumer(ILogger<UserSubscribedConsumer> logger, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        public async Task Consume(ConsumeContext<UserSubscribedEvent> context)
        {
            var message = context.Message;
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();

            // For now, let's assume Admin has a fixed GUID or we find them.
            // Placeholder: Notifying a generic ID or logging
            _logger.LogInformation($"[ADMIN NOTIFY] New Newsletter Subscriber: {message.Email}");
        }
    }
}
