using InkWell.NotificationService.Data;
using InkWell.NotificationService.Models;
using InkWell.Shared.Events;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace InkWell.NotificationService.Consumers
{
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
                Title = "Post Published",
                Message = $"Your story '{message.Title}' is now live! 🚀",
                Type = "Post",
                CreatedAt = DateTime.UtcNow,
                RelatedId = message.PostId.ToString()
            };
            dbContext.Notifications.Add(authorNotification);

            // 🚀 2. GLOBAL BROADCAST: Notify ALL other users
            var otherUsers = await dbContext.NotificationUsers
                .Where(u => u.UserId != message.AuthorId)
                .ToListAsync();

            _logger.LogInformation("Broadcasting new post notification to {Count} users", otherUsers.Count);

            foreach (var user in otherUsers)
            {
                dbContext.Notifications.Add(new Notification
                {
                    NotificationId = Guid.NewGuid(),
                    UserId = user.UserId,
                    Title = "New Story Published",
                    Message = $"Check out '{message.Title}' by {message.AuthorName}!",
                    Type = "Post",
                    CreatedAt = DateTime.UtcNow,
                    RelatedId = message.PostId.ToString()
                });
            }

            await dbContext.SaveChangesAsync();
            _logger.LogInformation($"[BROADCAST] Notification completed for Post: {message.Title}");
        }
    }
}
