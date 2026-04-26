using InkWell.Shared.Events;
using MassTransit;
using InkWell.NewsletterService.Data;
using Microsoft.EntityFrameworkCore;
using InkWell.Shared.Services;

namespace InkWell.NewsletterService.Consumers
{
    public class PostCreatedConsumer : IConsumer<PostCreatedEvent>
    {
        private readonly NewsletterDbContext _dbContext;
        private readonly ILogger<PostCreatedConsumer> _logger;
        private readonly IEmailService _emailService;

        public PostCreatedConsumer(NewsletterDbContext dbContext, ILogger<PostCreatedConsumer> logger, IEmailService emailService)
        {
            _dbContext = dbContext;
            _logger = logger;
            _emailService = emailService;
        }

        public async Task Consume(ConsumeContext<PostCreatedEvent> context)
        {
            var message = context.Message;
            _logger.LogInformation($"[REAL-TIME EMAIL] Processing new post: {message.Title}");

            // 1. Get all active subscribers
            var subscribers = await _dbContext.Subscribers
                .Where(s => s.Status == "Active")
                .ToListAsync();

            _logger.LogInformation($"[REAL-TIME EMAIL] Found {subscribers.Count} active subscribers.");

            // 2. Send email to each subscriber
            foreach (var sub in subscribers)
            {
                var subject = $"New Post: {message.Title}";
                var body = $@"
                    <div style='font-family: Arial, sans-serif; padding: 20px; color: #333;'>
                        <h2 style='color: #4A90E2;'>New Story from InkWell!</h2>
                        <p>Hi there,</p>
                        <p>A new post has just been published: <strong>{message.Title}</strong></p>
                        <br/>
                        <a href='http://localhost:4200/posts/{message.Slug}' style='background-color: #4A90E2; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Read More</a>
                        <br/><br/>
                        <p>Best regards,<br/>The InkWell Team</p>
                    </div>";

                await _emailService.SendEmailAsync(sub.Email, subject, body);
            }

            _logger.LogInformation($"[REAL-TIME EMAIL] All emails processed for: {message.Title}");
        }
    }
}
