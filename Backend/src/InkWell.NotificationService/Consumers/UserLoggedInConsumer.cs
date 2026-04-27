using InkWell.NotificationService.Data;
using InkWell.NotificationService.Models;
using InkWell.Shared.Events;
using MassTransit;

namespace InkWell.NotificationService.Consumers
{
    public class UserLoggedInConsumer : IConsumer<UserLoggedInEvent>
    {
        private readonly ILogger<UserLoggedInConsumer> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public UserLoggedInConsumer(ILogger<UserLoggedInConsumer> logger, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        public async Task Consume(ConsumeContext<UserLoggedInEvent> context)
        {
            var message = context.Message;
            
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();

            // 🚀 Sync user info on every login
            var user = await dbContext.NotificationUsers.FindAsync(message.UserId);
            if (user == null)
            {
                user = new NotificationUser { UserId = message.UserId };
                dbContext.NotificationUsers.Add(user);
            }
            
            user.FullName = message.FullName;
            user.Role = message.Role;
            user.LastSeen = DateTime.UtcNow;

            await dbContext.SaveChangesAsync();
            _logger.LogInformation("User {UserId} activity synced on login", message.UserId);
        }
    }
}
