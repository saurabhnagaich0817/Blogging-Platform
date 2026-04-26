using InkWell.NewsletterService.DTOs;
using InkWell.NewsletterService.Models;
using InkWell.NewsletterService.Repositories;
using Microsoft.Extensions.Logging;
using MassTransit;
using InkWell.Shared.Events;
using InkWell.Shared.Services;

namespace InkWell.NewsletterService.Services
{
    /// <summary>
    /// Service responsible for managing newsletter subscriptions, 
    /// subscription approvals, and email broadcasting.
    /// </summary>
    public class NewsletterService : INewsletterService
    {
        private readonly ISubscriberRepository _repository;
        private readonly ILogger<NewsletterService> _logger;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly IEmailService _emailService;

        public NewsletterService(ISubscriberRepository repository, ILogger<NewsletterService> logger, IPublishEndpoint publishEndpoint, IEmailService emailService)
        {
            _repository = repository;
            _logger = logger;
            _publishEndpoint = publishEndpoint;
            _emailService = emailService;
        }

        /// <summary>
        /// Registers a new subscription request and sends a confirmation email.
        /// </summary>
        public async Task<string> SubscribeAsync(SubscribeDTO dto)
        {
            var existing = await _repository.GetSubscriberByEmailAsync(dto.Email);
            
            if (existing != null)
            {
                if (existing.Status == "Active") return "Already subscribed.";
                if (existing.Status == "Pending") return "Your subscription is currently waiting for Admin approval.";
            }

            var subscriber = new Subscriber
            {
                SubscriberId = Guid.NewGuid(),
                Email = dto.Email,
                FullName = dto.FullName,
                UserId = dto.UserId,
                Status = "Pending",
                Token = Guid.NewGuid(),
                SubscribedAt = DateTime.UtcNow
            };

            await _repository.AddSubscriberAsync(subscriber);
            
            // Send confirmation email (mock for now but using real service)
            await SendConfirmationEmailAsync(subscriber.Email, subscriber.FullName ?? "Subscriber", subscriber.Token);

            // Publish Event for Notifications (to Admins)
            await _publishEndpoint.Publish(new NotificationEvent
            {
                UserId = Guid.Empty, // Broadcast/Admin
                Title = "New Newsletter Subscription",
                Type = "System",
                Message = $"{subscriber.FullName} ({subscriber.Email}) has requested to subscribe to the newsletter.",
                ReferenceId = subscriber.SubscriberId.ToString()
            });

            return "Wait for admin approval. Once approved, you will be subscribed!";
        }

        public async Task<bool> ConfirmSubscriptionAsync(Guid token)
        {
            var subscriber = await _repository.GetSubscriberByTokenAsync(token);
            if (subscriber == null || subscriber.Status != "Pending") return false;

            subscriber.Status = "Active";
            // Regenerate token so it can be used securely for unsubscribe links in future emails
            subscriber.Token = Guid.NewGuid(); 
            
            await _repository.UpdateSubscriberAsync(subscriber);

            // Notify User via real email
            await _emailService.SendEmailAsync(subscriber.Email, "InkWell Newsletter Confirmed", 
                $"<p>Hi {subscriber.FullName}, your subscription is now active! Welcome aboard.</p>");

            return true;
        }

        public async Task<bool> UnsubscribeAsync(Guid token)
        {
            var subscriber = await _repository.GetSubscriberByTokenAsync(token);
            if (subscriber == null || subscriber.Status == "Unsubscribed") return false;

            subscriber.Status = "Unsubscribed";
            subscriber.UnsubscribedAt = DateTime.UtcNow;
            
            await _repository.UpdateSubscriberAsync(subscriber);
            return true;
        }

        public async Task<IEnumerable<SubscriberResponseDTO>> GetAllSubscribersAsync()
        {
            var subscribers = await _repository.GetAllSubscribersAsync();
            return subscribers.Select(s => new SubscriberResponseDTO
            {
                SubscriberId = s.SubscriberId,
                Email = s.Email,
                FullName = s.FullName,
                Status = s.Status,
                SubscribedAt = s.SubscribedAt
            });
        }

        public async Task<bool> ApproveSubscriberAsync(Guid id)
        {
            var subscriber = await _repository.GetSubscriberByIdAsync(id);
            if (subscriber == null) return false;

            subscriber.Status = "Active";
            await _repository.UpdateSubscriberAsync(subscriber);

            // Send Real Email
            var body = $@"
                <div style='font-family: Arial, sans-serif; padding: 20px;'>
                    <h2 style='color: #4A90E2;'>Subscription Approved!</h2>
                    <p>Hi {subscriber.FullName},</p>
                    <p>Your newsletter subscription has been approved! You will now receive our latest updates.</p>
                    <br/>
                    <p>Best regards,<br/>The InkWell Team</p>
                </div>";
            await _emailService.SendEmailAsync(subscriber.Email, "InkWell Newsletter Subscription Approved", body);

            // Notify User in-app
            if (subscriber.UserId.HasValue)
            {
                await _publishEndpoint.Publish(new NotificationEvent
                {
                    UserId = subscriber.UserId.Value,
                    Title = "Subscription Approved",
                    Type = "System",
                    Message = "Your newsletter subscription has been approved! You will now receive our latest updates.",
                    ReferenceId = null,
                    UserEmail = subscriber.Email
                });
            }

            return true;
        }

        public async Task<bool> RejectSubscriberAsync(Guid id)
        {
            var subscriber = await _repository.GetSubscriberByIdAsync(id);
            if (subscriber == null) return false;

            subscriber.Status = "Rejected";
            await _repository.UpdateSubscriberAsync(subscriber);
            return true;
        }

        private async Task SendConfirmationEmailAsync(string email, string name, Guid token)
        {
            var link = $"http://localhost:5070/api/newsletter/confirm?token={token}";
            var subject = "Confirm your InkWell Subscription";
            var body = $@"
                <div style='font-family: Arial, sans-serif; padding: 20px;'>
                    <h2>Confirm your Subscription</h2>
                    <p>Hi {name},</p>
                    <p>Thanks for subscribing to InkWell! Please confirm your email by clicking the link below:</p>
                    <a href='{link}' style='display: inline-block; padding: 10px 20px; background-color: #4A90E2; color: white; text-decoration: none; border-radius: 5px;'>Confirm Subscription</a>
                    <br/><br/>
                    <p>If you didn't request this, you can safely ignore this email.</p>
                </div>";

            await _emailService.SendEmailAsync(email, subject, body);
        }
    }
}
