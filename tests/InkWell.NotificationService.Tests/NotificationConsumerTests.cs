using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using FluentAssertions;
using InkWell.NotificationService.Consumers;
using InkWell.NotificationService.Data;
using InkWell.NotificationService.Models;
using InkWell.Shared.Events;
using InkWell.Shared.Services;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InkWell.NotificationService.Tests
{
    [TestClass]
    public class NotificationConsumerTests
    {
        private Mock<IServiceScopeFactory>? _scopeFactoryMock;
        private Mock<IEmailService>? _emailServiceMock;
        private NotificationDbContext? _dbContext;

        [TestInitialize]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<NotificationDbContext>()
                .UseInMemoryDatabase(databaseName: "NotificationTestDb_" + Guid.NewGuid())
                .Options;

            _dbContext = new NotificationDbContext(options);
            _scopeFactoryMock = new Mock<IServiceScopeFactory>();
            _emailServiceMock = new Mock<IEmailService>();
            
            var scopeMock = new Mock<IServiceScope>();
            var serviceProviderMock = new Mock<IServiceProvider>();

            _scopeFactoryMock.Setup(x => x.CreateScope()).Returns(scopeMock.Object);
            scopeMock.Setup(x => x.ServiceProvider).Returns(serviceProviderMock.Object);
            serviceProviderMock.Setup(x => x.GetService(typeof(NotificationDbContext))).Returns(_dbContext);
            serviceProviderMock.Setup(x => x.GetService(typeof(IEmailService))).Returns(_emailServiceMock.Object);
        }

        // [TEST 1]: PostCreatedConsumer - Notify Author.
        [TestMethod]
        public async Task PostCreated_ShouldNotifyAuthor()
        {
            var authorId = Guid.NewGuid();
            _dbContext!.NotificationUsers.Add(new NotificationUser { UserId = authorId, FullName = "Saurabh" });
            await _dbContext.SaveChangesAsync();

            var consumer = new PostCreatedConsumer(new Mock<ILogger<PostCreatedConsumer>>().Object, _scopeFactoryMock!.Object);
            var contextMock = new Mock<ConsumeContext<PostCreatedEvent>>();
            contextMock.Setup(x => x.Message).Returns(new PostCreatedEvent { PostId = Guid.NewGuid(), Title = "Blog", AuthorId = authorId });

            await consumer.Consume(contextMock.Object);

            var notif = await _dbContext.Notifications.FirstOrDefaultAsync(n => n.UserId == authorId);
            // Title should match exactly what's in the Consumer code.
            notif!.Title.Should().Be("Post Published");
        }

        // [TEST 2]: PostCreatedConsumer - Notify Other Users.
        [TestMethod]
        public async Task PostCreated_ShouldNotifyOtherUsers()
        {
            var user1 = Guid.NewGuid();
            _dbContext!.NotificationUsers.Add(new NotificationUser { UserId = user1, FullName = "User1" });
            await _dbContext.SaveChangesAsync();

            var consumer = new PostCreatedConsumer(new Mock<ILogger<PostCreatedConsumer>>().Object, _scopeFactoryMock!.Object);
            var contextMock = new Mock<ConsumeContext<PostCreatedEvent>>();
            contextMock.Setup(x => x.Message).Returns(new PostCreatedEvent { AuthorId = Guid.NewGuid(), Title = "Blog", AuthorName = "Saurabh" });

            await consumer.Consume(contextMock.Object);

            var notif = await _dbContext.Notifications.FirstOrDefaultAsync(n => n.UserId == user1);
            notif!.Title.Should().Be("New Story Published");
            notif.Message.Should().Contain("Saurabh");
        }

        // [TEST 3]: CommentAddedConsumer - Notify Post Author.
        [TestMethod]
        public async Task CommentAdded_ShouldNotifyPostAuthor()
        {
            var postAuthorId = Guid.NewGuid();
            var consumer = new CommentAddedConsumer(new Mock<ILogger<CommentAddedConsumer>>().Object, _scopeFactoryMock!.Object);
            var contextMock = new Mock<ConsumeContext<CommentAddedEvent>>();
            contextMock.Setup(x => x.Message).Returns(new CommentAddedEvent { PostAuthorId = postAuthorId, ContentPreview = "Nice!", AuthorId = Guid.NewGuid() });

            await consumer.Consume(contextMock.Object);

            var notif = await _dbContext.Notifications.FirstOrDefaultAsync(n => n.UserId == postAuthorId);
            notif!.Title.Should().Be("New Comment");
        }

        // [TEST 4]: UserRegisteredConsumer - Welcome Notification & Sync.
        [TestMethod]
        public async Task UserRegistered_ShouldSyncAndSendEmail()
        {
            var userId = Guid.NewGuid();
            var mailSettings = Options.Create(new MailSettings { AdminEmail = "admin@inkwell.com" });
            var consumer = new UserRegisteredConsumer(
                new Mock<ILogger<UserRegisteredConsumer>>().Object, 
                _emailServiceMock!.Object, 
                mailSettings, 
                _scopeFactoryMock!.Object);

            var contextMock = new Mock<ConsumeContext<UserRegisteredEvent>>();
            contextMock.Setup(x => x.Message).Returns(new UserRegisteredEvent { UserId = userId, FullName = "New User", Email = "test@test.com", Role = "Author" });

            await consumer.Consume(contextMock.Object);

            var user = await _dbContext!.NotificationUsers.FirstOrDefaultAsync(u => u.UserId == userId);
            user.Should().NotBeNull();
            user!.FullName.Should().Be("New User");
        }

        // [TEST 5]: UserLoggedInConsumer - Activity Sync.
        [TestMethod]
        public async Task UserLoggedIn_ShouldSyncUserActivity()
        {
            var userId = Guid.NewGuid();
            var consumer = new UserLoggedInConsumer(new Mock<ILogger<UserLoggedInConsumer>>().Object, _scopeFactoryMock!.Object);
            var contextMock = new Mock<ConsumeContext<UserLoggedInEvent>>();
            contextMock.Setup(x => x.Message).Returns(new UserLoggedInEvent { UserId = userId, FullName = "Tester", Role = "Reader" });

            await consumer.Consume(contextMock.Object);

            var user = await _dbContext!.NotificationUsers.FirstOrDefaultAsync(u => u.UserId == userId);
            user.Should().NotBeNull();
            user!.FullName.Should().Be("Tester");
        }

        // [TEST 6]: PostLikedConsumer - Notify Author.
        [TestMethod]
        public async Task PostLiked_ShouldNotifyAuthor()
        {
            var authorId = Guid.NewGuid();
            var consumer = new PostLikedConsumer(new Mock<ILogger<PostLikedConsumer>>().Object, _scopeFactoryMock!.Object);
            var contextMock = new Mock<ConsumeContext<PostLikedEvent>>();
            contextMock.Setup(x => x.Message).Returns(new PostLikedEvent { PostId = Guid.NewGuid(), LikerName = "Fan", PostAuthorId = authorId });

            await consumer.Consume(contextMock.Object);

            var notif = await _dbContext!.Notifications.FirstOrDefaultAsync(n => n.UserId == authorId);
            notif!.Title.Should().Be("New Like");
        }

        // [TEST 7]: NotificationEventConsumer - Targeted System alert.
        [TestMethod]
        public async Task NotificationEvent_ShouldCreateTargetedNotification()
        {
            var userId = Guid.NewGuid();
            var consumer = new NotificationEventConsumer(new Mock<ILogger<NotificationEventConsumer>>().Object, _scopeFactoryMock!.Object);
            var contextMock = new Mock<ConsumeContext<NotificationEvent>>();
            contextMock.Setup(x => x.Message).Returns(new NotificationEvent { UserId = userId, Title = "Sys", Message = "Msg" });

            await consumer.Consume(contextMock.Object);

            var notif = await _dbContext!.Notifications.FirstOrDefaultAsync(n => n.UserId == userId);
            notif!.Message.Should().Be("Msg");
        }

        // [TEST 8]: NotificationEventConsumer - Admin Broadcast.
        [TestMethod]
        public async Task NotificationEvent_ShouldBroadcastToAdmins()
        {
            var adminId = Guid.NewGuid();
            _dbContext!.NotificationUsers.Add(new NotificationUser { UserId = adminId, Role = "Admin" });
            await _dbContext.SaveChangesAsync();

            var consumer = new NotificationEventConsumer(new Mock<ILogger<NotificationEventConsumer>>().Object, _scopeFactoryMock!.Object);
            var contextMock = new Mock<ConsumeContext<NotificationEvent>>();
            contextMock.Setup(x => x.Message).Returns(new NotificationEvent { UserId = Guid.Empty, Title = "Broadcast", Message = "Hello Admins" });

            await consumer.Consume(contextMock.Object);

            var notif = await _dbContext.Notifications.FirstOrDefaultAsync(n => n.UserId == adminId);
            notif.Should().NotBeNull();
            notif!.Title.Should().Be("Broadcast");
        }

        // [TEST 9]: CommentAdded - Logic check (Self-comment also creates notification in current code).
        [TestMethod]
        public async Task CommentAdded_ShouldCreateNotification()
        {
            var authorId = Guid.NewGuid();
            var consumer = new CommentAddedConsumer(new Mock<ILogger<CommentAddedConsumer>>().Object, _scopeFactoryMock!.Object);
            var contextMock = new Mock<ConsumeContext<CommentAddedEvent>>();
            contextMock.Setup(x => x.Message).Returns(new CommentAddedEvent { PostAuthorId = authorId, AuthorId = authorId, ContentPreview = "Self" });

            await consumer.Consume(contextMock.Object);

            var count = await _dbContext!.Notifications.CountAsync(n => n.UserId == authorId);
            count.Should().Be(1);
        }

        // [TEST 10]: Multi-event stacking.
        [TestMethod]
        public async Task MultipleEvents_ShouldStack()
        {
            var userId = Guid.NewGuid();
            var consumer = new NotificationEventConsumer(new Mock<ILogger<NotificationEventConsumer>>().Object, _scopeFactoryMock!.Object);
            var contextMock = new Mock<ConsumeContext<NotificationEvent>>();
            contextMock.Setup(x => x.Message).Returns(new NotificationEvent { UserId = userId, Title = "Alert" });

            await consumer.Consume(contextMock.Object);
            await consumer.Consume(contextMock.Object);

            var count = await _dbContext!.Notifications.CountAsync(n => n.UserId == userId);
            count.Should().Be(2);
        }

        // [TEST 11]: Unread status check.
        [TestMethod]
        public async Task NewNotification_ShouldBeUnread()
        {
            var authorId = Guid.NewGuid();
            _dbContext!.NotificationUsers.Add(new NotificationUser { UserId = authorId, FullName = "Test" });
            await _dbContext.SaveChangesAsync();

            var consumer = new PostCreatedConsumer(new Mock<ILogger<PostCreatedConsumer>>().Object, _scopeFactoryMock!.Object);
            var contextMock = new Mock<ConsumeContext<PostCreatedEvent>>();
            contextMock.Setup(x => x.Message).Returns(new PostCreatedEvent { PostId = Guid.NewGuid(), Title = "Blog", AuthorId = authorId });

            await consumer.Consume(contextMock.Object);

            var notif = await _dbContext.Notifications.FirstAsync();
            notif.IsRead.Should().BeFalse();
        }

        // [TEST 12]: Data Integrity - RelatedId storage.
        [TestMethod]
        public async Task CommentAdded_ShouldStorePostId_AsRelatedId()
        {
            var postId = Guid.NewGuid();
            var consumer = new CommentAddedConsumer(new Mock<ILogger<CommentAddedConsumer>>().Object, _scopeFactoryMock!.Object);
            var contextMock = new Mock<ConsumeContext<CommentAddedEvent>>();
            contextMock.Setup(x => x.Message).Returns(new CommentAddedEvent { PostAuthorId = Guid.NewGuid(), PostId = postId });

            await consumer.Consume(contextMock.Object);

            var notif = await _dbContext!.Notifications.FirstAsync();
            notif.RelatedId.Should().Be(postId.ToString());
        }

        // [TEST 13]: Message Preview logic.
        [TestMethod]
        public async Task CommentAdded_ShouldUseContentPreview()
        {
            var consumer = new CommentAddedConsumer(new Mock<ILogger<CommentAddedConsumer>>().Object, _scopeFactoryMock!.Object);
            var contextMock = new Mock<ConsumeContext<CommentAddedEvent>>();
            contextMock.Setup(x => x.Message).Returns(new CommentAddedEvent { PostAuthorId = Guid.NewGuid(), ContentPreview = "Hello World" });

            await consumer.Consume(contextMock.Object);

            var notif = await _dbContext!.Notifications.FirstAsync();
            notif.Message.Should().Contain("Hello World");
        }

        // [TEST 14]: Broadcast - Ensure notifications are created for other users.
        [TestMethod]
        public async Task Broadcast_ShouldNotifyOtherUsers()
        {
            var authorId = Guid.NewGuid();
            var user1 = Guid.NewGuid();
            _dbContext!.NotificationUsers.AddRange(new List<NotificationUser> { 
                new NotificationUser { UserId = authorId, FullName = "Author" }, 
                new NotificationUser { UserId = user1, FullName = "Follower" } 
            });
            await _dbContext.SaveChangesAsync();

            var consumer = new PostCreatedConsumer(new Mock<ILogger<PostCreatedConsumer>>().Object, _scopeFactoryMock!.Object);
            var contextMock = new Mock<ConsumeContext<PostCreatedEvent>>();
            contextMock.Setup(x => x.Message).Returns(new PostCreatedEvent { AuthorId = authorId, Title = "Broadcast" });

            await consumer.Consume(contextMock.Object);

            // 1 for author, 1 for user1
            var count = await _dbContext.Notifications.CountAsync();
            count.Should().Be(2);
        }
    }
}
