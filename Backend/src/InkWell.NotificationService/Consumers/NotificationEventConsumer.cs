using InkWell.NotificationService.Data;
using InkWell.NotificationService.Models;
using InkWell.Shared.Events;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace InkWell.NotificationService.Consumers
{
    public class NotificationEventConsumer : IConsumer<NotificationEvent>
    {
        private readonly ILogger<NotificationEventConsumer> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public NotificationEventConsumer(ILogger<NotificationEventConsumer> logger, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        public async Task Consume(ConsumeContext<NotificationEvent> context)
        {
            var message = context.Message;
            
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();

            // 🚀 Case 1: Targeted Notification (Single User)
            if (message.UserId != Guid.Empty)
            {
                var notification = new Notification
                {
                    NotificationId = Guid.NewGuid(),
                    UserId = message.UserId,
                    Title = message.Title,
                    Message = message.Message,
                    Type = message.Type,
                    RelatedId = message.ReferenceId,
                    CreatedAt = DateTime.UtcNow
                };
                dbContext.Notifications.Add(notification);
                _logger.LogInformation("Stored targeted notification for user {UserId}", message.UserId);
            }
            // 🚀 Case 2: ADMIN BROADCAST (UserId is Empty)
            else
            {
                var admins = await dbContext.NotificationUsers
                    .Where(u => u.Role == "Admin")
                    .ToListAsync();

                _logger.LogInformation("Broadcasting admin notification to {Count} admins", admins.Count);

                foreach (var admin in admins)
                {
                    dbContext.Notifications.Add(new Notification
                    {
                        NotificationId = Guid.NewGuid(),
                        UserId = admin.UserId,
                        Title = message.Title,
                        Message = message.Message,
                        Type = message.Type,
                        RelatedId = message.ReferenceId,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            await dbContext.SaveChangesAsync();
        }
    }
}
