using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using FluentAssertions;
using InkWell.NewsletterService.Services;
using InkWell.NewsletterService.Repositories;
using InkWell.NewsletterService.DTOs;
using InkWell.NewsletterService.Models;
using Microsoft.Extensions.Logging;
using MassTransit;
using InkWell.Shared.Events;
using InkWell.Shared.Services;

namespace InkWell.NewsletterService.Tests
{
    [TestClass]
    public class NewsletterServiceTests
    {
        private Mock<ISubscriberRepository>? _repositoryMock;
        private Mock<ILogger<InkWell.NewsletterService.Services.NewsletterService>>? _loggerMock;
        private Mock<IPublishEndpoint>? _publishEndpointMock;
        private Mock<IEmailService>? _emailServiceMock;
        private InkWell.NewsletterService.Services.NewsletterService? _newsletterService;

        [TestInitialize]
        public void Setup()
        {
            _repositoryMock = new Mock<ISubscriberRepository>();
            _loggerMock = new Mock<ILogger<InkWell.NewsletterService.Services.NewsletterService>>();
            _publishEndpointMock = new Mock<IPublishEndpoint>();
            _emailServiceMock = new Mock<IEmailService>();

            _newsletterService = new InkWell.NewsletterService.Services.NewsletterService(
                _repositoryMock.Object,
                _loggerMock.Object,
                _publishEndpointMock.Object,
                _emailServiceMock.Object);
        }

        // [TEST 1]: Successful subscription request.
        [TestMethod]
        public async Task SubscribeAsync_ShouldCreatePendingSubscriber_AndSendEmail()
        {
            var dto = new SubscribeDTO { Email = "new@test.com", FullName = "Saurabh" };
            _repositoryMock!.Setup(repo => repo.GetSubscriberByEmailAsync(dto.Email)).ReturnsAsync((Subscriber?)null);

            var result = await _newsletterService!.SubscribeAsync(dto);

            result.Should().Contain("Wait for admin approval");
            _repositoryMock.Verify(repo => repo.AddSubscriberAsync(It.IsAny<Subscriber>()), Times.Once);
            _emailServiceMock!.Verify(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        // [TEST 2]: Prevent duplicate active subscriptions.
        [TestMethod]
        public async Task SubscribeAsync_ShouldReturnAlreadySubscribed_WhenStatusIsActive()
        {
            var email = "active@test.com";
            _repositoryMock!.Setup(repo => repo.GetSubscriberByEmailAsync(email)).ReturnsAsync(new Subscriber { Email = email, Status = "Active" });

            var result = await _newsletterService!.SubscribeAsync(new SubscribeDTO { Email = email });

            result.Should().Be("Already subscribed.");
        }

        // [TEST 3]: Inform user if subscription is already pending.
        [TestMethod]
        public async Task SubscribeAsync_ShouldReturnWaitingMessage_WhenStatusIsPending()
        {
            var email = "pending@test.com";
            _repositoryMock!.Setup(repo => repo.GetSubscriberByEmailAsync(email)).ReturnsAsync(new Subscriber { Email = email, Status = "Pending" });

            var result = await _newsletterService!.SubscribeAsync(new SubscribeDTO { Email = email });

            result.Should().Contain("waiting for Admin approval");
        }

        // [TEST 4]: Notify admin via event on new subscription.
        [TestMethod]
        public async Task SubscribeAsync_ShouldPublishNotificationEvent_ForAdmin()
        {
            var dto = new SubscribeDTO { Email = "admin-notify@test.com" };
            _repositoryMock!.Setup(repo => repo.GetSubscriberByEmailAsync(dto.Email)).ReturnsAsync((Subscriber?)null);

            await _newsletterService!.SubscribeAsync(dto);

            _publishEndpointMock!.Verify(p => p.Publish(It.IsAny<NotificationEvent>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        // [TEST 5]: Confirm subscription via token.
        [TestMethod]
        public async Task ConfirmSubscriptionAsync_ShouldSetActiveStatus_WhenTokenIsValid()
        {
            var token = Guid.NewGuid();
            var subscriber = new Subscriber { Email = "test@test.com", Status = "Pending", Token = token };
            _repositoryMock!.Setup(repo => repo.GetSubscriberByTokenAsync(token)).ReturnsAsync(subscriber);

            var result = await _newsletterService!.ConfirmSubscriptionAsync(token);

            result.Should().BeTrue();
            subscriber.Status.Should().Be("Active");
        }

        // [TEST 6]: Reject invalid tokens.
        [TestMethod]
        public async Task ConfirmSubscriptionAsync_ShouldReturnFalse_WhenTokenIsInvalid()
        {
            _repositoryMock!.Setup(repo => repo.GetSubscriberByTokenAsync(It.IsAny<Guid>())).ReturnsAsync((Subscriber?)null);

            var result = await _newsletterService!.ConfirmSubscriptionAsync(Guid.NewGuid());

            result.Should().BeFalse();
        }

        // [TEST 7]: Ensure token is regenerated for security after confirmation.
        [TestMethod]
        public async Task ConfirmSubscriptionAsync_ShouldRegenerateToken()
        {
            var token = Guid.NewGuid();
            var subscriber = new Subscriber { Status = "Pending", Token = token };
            _repositoryMock!.Setup(repo => repo.GetSubscriberByTokenAsync(token)).ReturnsAsync(subscriber);

            await _newsletterService!.ConfirmSubscriptionAsync(token);

            subscriber.Token.Should().NotBe(token);
        }

        // [TEST 8]: Unsubscribe logic.
        [TestMethod]
        public async Task UnsubscribeAsync_ShouldSetStatusToUnsubscribed()
        {
            var token = Guid.NewGuid();
            var subscriber = new Subscriber { Status = "Active", Token = token };
            _repositoryMock!.Setup(repo => repo.GetSubscriberByTokenAsync(token)).ReturnsAsync(subscriber);

            await _newsletterService!.UnsubscribeAsync(token);

            subscriber.Status.Should().Be("Unsubscribed");
        }

        // [TEST 9]: Fail if trying to unsubscribe an already unsubscribed user.
        [TestMethod]
        public async Task UnsubscribeAsync_ShouldReturnFalse_IfAlreadyUnsubscribed()
        {
            var token = Guid.NewGuid();
            _repositoryMock!.Setup(repo => repo.GetSubscriberByTokenAsync(token)).ReturnsAsync(new Subscriber { Status = "Unsubscribed" });

            var result = await _newsletterService!.UnsubscribeAsync(token);

            result.Should().BeFalse();
        }

        // [TEST 10]: Admin approval flow.
        [TestMethod]
        public async Task ApproveSubscriberAsync_ShouldSetActiveStatus_AndSendWelcomeEmail()
        {
            var subId = Guid.NewGuid();
            var subscriber = new Subscriber { SubscriberId = subId, Email = "approved@test.com", FullName = "Sub", Status = "Pending" };
            _repositoryMock!.Setup(repo => repo.GetSubscriberByIdAsync(subId)).ReturnsAsync(subscriber);

            await _newsletterService!.ApproveSubscriberAsync(subId);

            subscriber.Status.Should().Be("Active");
            _emailServiceMock!.Verify(e => e.SendEmailAsync(subscriber.Email, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        // [TEST 11]: Notify user in-app after admin approval.
        [TestMethod]
        public async Task ApproveSubscriberAsync_ShouldPublishNotificationEvent_ForUser()
        {
            var subId = Guid.NewGuid();
            var subscriber = new Subscriber { SubscriberId = subId, UserId = Guid.NewGuid(), Status = "Pending" };
            _repositoryMock!.Setup(repo => repo.GetSubscriberByIdAsync(subId)).ReturnsAsync(subscriber);

            await _newsletterService!.ApproveSubscriberAsync(subId);

            _publishEndpointMock!.Verify(p => p.Publish(It.IsAny<NotificationEvent>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        // [TEST 12]: Admin rejection flow.
        [TestMethod]
        public async Task RejectSubscriberAsync_ShouldSetStatusToRejected()
        {
            var subId = Guid.NewGuid();
            var subscriber = new Subscriber { Status = "Pending" };
            _repositoryMock!.Setup(repo => repo.GetSubscriberByIdAsync(subId)).ReturnsAsync(subscriber);

            await _newsletterService!.RejectSubscriberAsync(subId);

            subscriber.Status.Should().Be("Rejected");
        }

        // [TEST 13]: Fetch all subscribers.
        [TestMethod]
        public async Task GetAllSubscribersAsync_ShouldReturnList()
        {
            var subs = new List<Subscriber> { new Subscriber { Email = "s1@t.com" }, new Subscriber { Email = "s2@t.com" } };
            _repositoryMock!.Setup(repo => repo.GetAllSubscribersAsync()).ReturnsAsync(subs);

            var result = await _newsletterService!.GetAllSubscribersAsync();

            result.Count().Should().Be(2);
        }

        // [TEST 14]: Support subscribers without names (default to 'Subscriber').
        [TestMethod]
        public async Task SubscribeAsync_ShouldHandleNullFullName()
        {
            var dto = new SubscribeDTO { Email = "noname@test.com", FullName = null };
            _repositoryMock!.Setup(repo => repo.GetSubscriberByEmailAsync(dto.Email)).ReturnsAsync((Subscriber?)null);

            await _newsletterService!.SubscribeAsync(dto);

            _repositoryMock.Verify(repo => repo.AddSubscriberAsync(It.Is<Subscriber>(s => s.FullName == null)), Times.Once);
        }
    }
}
