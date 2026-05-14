using InkWell.NotificationService.Data;
using InkWell.NotificationService.Models;
using InkWell.Shared.Events;
using MassTransit;
using InkWell.Shared.Services;

namespace InkWell.NotificationService.Consumers
{
    /// <summary>
    /// Ye consumer tab trigger hota hai jab koi user kisi post ko "Like" karta hai.
    /// MassTransit automatically RabbitMQ se message pick karke yahan bhej deta hai.
    /// </summary>
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
            // RabbitMQ se jo data aaya hai use pick karte hain
            var message = context.Message;
            
            // Database operation ke liye scope create kar rahe hain taaki memory leaks na hon
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();

            // Naya notification object bana rahe hain jo user ko frontend par dikhega
            var notification = new Notification
            {
                NotificationId = Guid.NewGuid(),
                UserId = message.PostAuthorId, // Notification usko jayegi jisne post likhi hai
                Title = "New Like",
                Message = $"{message.LikerName} liked your post!",
                Type = "Like",
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                RelatedId = message.PostId.ToString() // Post ka ID save kar rahe hain taaki click karne par wahan le ja sakein
            };

            // Database mein save kar rahe hain
            dbContext.Notifications.Add(notification);
            await dbContext.SaveChangesAsync();
            _logger.LogInformation($"[NOTIFICATION SAVED] Like notification for User {message.PostAuthorId}");

            // Agar author ka email available hai, toh usey real-time email bhi bhej dete hain
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
                
                // Background mein email bhej rahe hain
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
