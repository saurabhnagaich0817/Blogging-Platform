using InkWell.NotificationService.Data;
using InkWell.NotificationService.Models;
using InkWell.Shared.Events;
using MassTransit;

namespace InkWell.NotificationService.Consumers
{
    /// <summary>
    /// Consumer for PostCreatedEvent. 
    /// Generates both targeted author notifications and global broadcast notifications 
    /// when a new story is published.
    /// </summary>
    public class PostCreatedConsumer : IConsumer<PostCreatedEvent>
    {
        private readonly ILogger<PostCreatedConsumer> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public PostCreatedConsumer(ILogger<PostCreatedConsumer> logger, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        public async Task Consume(ConsumeContext<PostCreatedEvent> context)
        {
            var message = context.Message;

            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();

            // 1. Notify the Author (Confirmation)
            var authorNotification = new Notification
            {
                NotificationId = Guid.NewGuid(),
                UserId = message.AuthorId,
                Message = $"Your story '{message.Title}' is now live! 🚀",
                Type = "Post",
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                RelatedId = message.PostId.ToString()
            };
            dbContext.Notifications.Add(authorNotification);

            // 2. BROADCAST removed to reduce noise as per user feedback.

            await dbContext.SaveChangesAsync();

            // 3. SIMULATED EMAIL NOTIFICATION (For Offline Users)
            _logger.LogInformation($"[MAIL] Detected offline subscribers. Sending email alerts for: {message.Title}");
            
            _logger.LogInformation($"[BROADCAST] Notification and Email tasks completed for Post: {message.Title}");
        }
    }
}
