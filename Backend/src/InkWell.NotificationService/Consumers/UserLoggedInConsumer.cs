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

            // 🚀 Sync user info on every login (Upsert pattern)
            var user = await dbContext.NotificationUsers.FindAsync(message.UserId);
            
            if (user == null)
            {
                try 
                {
                    user = new NotificationUser 
                    { 
                        UserId = message.UserId,
                        FullName = message.FullName,
                        Role = message.Role,
                        LastSeen = DateTime.UtcNow
                    };
                    dbContext.NotificationUsers.Add(user);
                    await dbContext.SaveChangesAsync();
                }
                catch (Exception)
                {
                    // If insert fails (maybe due to concurrent sync), try to fetch again and update
                    dbContext.ChangeTracker.Clear();
                    user = await dbContext.NotificationUsers.FindAsync(message.UserId);
                    if (user != null)
                    {
                        user.FullName = message.FullName;
                        user.Role = message.Role;
                        user.LastSeen = DateTime.UtcNow;
                        await dbContext.SaveChangesAsync();
                    }
                }
            }
            else
            {
                user.FullName = message.FullName;
                user.Role = message.Role;
                user.LastSeen = DateTime.UtcNow;
                await dbContext.SaveChangesAsync();
            }

            _logger.LogInformation("User {UserId} activity synced on login", message.UserId);
        }
    }
}
