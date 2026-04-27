using InkWell.Shared.Events;
using MassTransit;
using InkWell.Shared.Services;
using Microsoft.Extensions.Options;
using InkWell.NotificationService.Data;
using InkWell.NotificationService.Models;
using Microsoft.EntityFrameworkCore;

namespace InkWell.NotificationService.Consumers
{
    public class UserRegisteredConsumer : IConsumer<UserRegisteredEvent>
    {
        private readonly ILogger<UserRegisteredConsumer> _logger;
        private readonly IEmailService _emailService;
        private readonly MailSettings _mailSettings;
        private readonly IServiceScopeFactory _scopeFactory;

        public UserRegisteredConsumer(
            ILogger<UserRegisteredConsumer> logger, 
            IEmailService emailService, 
            IOptions<MailSettings> mailSettings,
            IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _emailService = emailService;
            _mailSettings = mailSettings.Value;
            _scopeFactory = scopeFactory;
        }

        public async Task Consume(ConsumeContext<UserRegisteredEvent> context)
        {
            var message = context.Message;
            
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();

            // 🚀 Sync user to NotificationUsers table
            var existing = await dbContext.NotificationUsers.FindAsync(message.UserId);
            if (existing == null)
            {
                dbContext.NotificationUsers.Add(new NotificationUser
                {
                    UserId = message.UserId,
                    FullName = message.FullName,
                    Role = message.Role,
                    LastSeen = DateTime.UtcNow
                });
                await dbContext.SaveChangesAsync();
                _logger.LogInformation("Synced new user {UserId} to Notification database", message.UserId);
            }

            // 1. Send Welcome Email to User
            try 
            {
                _logger.LogInformation($"[EMAIL EVENT] Sending Welcome Email to: {message.Email}");
                var userSubject = $"Welcome to InkWell, {message.FullName}!";
                var userBody = $@"<h2>Welcome to InkWell!</h2><p>Hi <strong>{message.FullName}</strong>, thrill to have you as a {message.Role}.</p>";
                await _emailService.SendEmailAsync(message.Email, userSubject, userBody);
            }
            catch(Exception ex) { _logger.LogError("Welcome email failed: {Msg}", ex.Message); }

            // 2. Send Notification Email to Admin
            if (!string.IsNullOrEmpty(_mailSettings.AdminEmail))
            {
                try 
                {
                    var adminSubject = "New User Registration Alert";
                    var adminBody = $"<p>New user registered: {message.FullName} ({message.Email}) as {message.Role}.</p>";
                    await _emailService.SendEmailAsync(_mailSettings.AdminEmail, adminSubject, adminBody);
                }
                catch(Exception ex) { _logger.LogError("Admin registration email failed: {Msg}", ex.Message); }
            }
        }
    }
}
