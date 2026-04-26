using InkWell.NotificationService.Data;
using InkWell.NotificationService.Models;
using InkWell.Shared.Events;
using MassTransit;
using InkWell.Shared.Services;

namespace InkWell.NotificationService.Consumers
{
    public class CommentAddedConsumer : IConsumer<CommentAddedEvent>
    {
        private readonly ILogger<CommentAddedConsumer> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public CommentAddedConsumer(ILogger<CommentAddedConsumer> logger, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        public async Task Consume(ConsumeContext<CommentAddedEvent> context)
        {
            var message = context.Message;
            
            // Create a scope to resolve scoped services like DbContext
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();

            var notification = new Notification
            {
                NotificationId = Guid.NewGuid(),
                UserId = message.PostAuthorId, // Recipient is the post author
                Message = $"Someone commented on your post: \"{message.ContentPreview}\"",
                Type = "Comment",
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                RelatedId = message.PostId.ToString()
            };

            dbContext.Notifications.Add(notification);
            await dbContext.SaveChangesAsync();

            _logger.LogInformation($"[NOTIFICATION SAVED] Comment notification for User {message.PostAuthorId}");

            // Send Email if Post Author Email is available
            if (!string.IsNullOrEmpty(message.PostAuthorEmail))
            {
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
                var subject = "New comment on your post!";
                var body = $@"
                    <div style='font-family: Arial, sans-serif; padding: 20px; color: #333;'>
                        <h2 style='color: #4A90E2;'>New Comment</h2>
                        <p>Someone just commented on your post on InkWell:</p>
                        <blockquote style='border-left: 4px solid #4A90E2; padding-left: 10px; font-style: italic;'>
                            ""{message.ContentPreview}""
                        </blockquote>
                        <br/>
                        <p>Best regards,<br/>The InkWell Team</p>
                    </div>";
                await emailService.SendEmailAsync(message.PostAuthorEmail, subject, body);
            }
        }
    }
}
