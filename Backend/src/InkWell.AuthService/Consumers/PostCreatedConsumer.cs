using InkWell.AuthService.Repositories;
using InkWell.Shared.Events;
using MassTransit;

namespace InkWell.AuthService.Consumers
{
    public class PostCreatedConsumer : IConsumer<PostCreatedEvent>
    {
        private readonly IUserRepository _userRepository;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly ILogger<PostCreatedConsumer> _logger;

        public PostCreatedConsumer(IUserRepository userRepository, IPublishEndpoint publishEndpoint, ILogger<PostCreatedConsumer> logger)
        {
            _userRepository = userRepository;
            _publishEndpoint = publishEndpoint;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<PostCreatedEvent> context)
        {
            var message = context.Message;
            _logger.LogInformation($"[SOCIAL ENGINE] Processing new post notification for Author: {message.AuthorId}");

            // 1. Get all users who follow/connected with this author
            var connections = await _userRepository.GetUserConnectionsAsync(message.AuthorId);
            var followers = connections
                .Where(c => c.Status == "Accepted")
                .Select(c => c.RequesterId == message.AuthorId ? c.ReceiverId : c.RequesterId)
                .Distinct()
                .ToList();

            _logger.LogInformation($"[SOCIAL ENGINE] Found {followers.Count} followers to notify.");

            // 2. Send notification to each follower
            foreach (var followerId in followers)
            {
                await _publishEndpoint.Publish(new NotificationEvent
                {
                    UserId = followerId,
                    Title = "New Story from Author!",
                    Type = "Social",
                    Message = $"An author you follow just published: '{message.Title}'",
                    ReferenceId = message.PostId.ToString()
                });
            }
        }
    }
}
