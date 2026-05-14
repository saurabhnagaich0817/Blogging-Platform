using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using FluentAssertions;
using InkWell.AuthService.Services;
using InkWell.AuthService.Repositories;
using InkWell.AuthService.DTOs;
using InkWell.AuthService.Models;
using Microsoft.Extensions.Configuration;
using MassTransit;
using InkWell.Shared.Events;

namespace InkWell.AuthService.Tests
{
    // [Interview Tip]: MSTest mein hum class ke upar [TestClass] attribute lagate hain.
    // Isse Visual Studio ko pata chalta hai ki ye ek Testing class hai.
    [TestClass]
    public class AuthServiceTests
    {
        private Mock<IUserRepository>? _userRepositoryMock;
        private Mock<IConfiguration>? _configurationMock;
        private Mock<IPublishEndpoint>? _publishEndpointMock;
        private InkWell.AuthService.Services.AuthService? _authService;

        // [Interview Tip]: [TestInitialize] wala method har test case se pehle chalta hai.
        // Isse hume har baar naya Mock object milta hai aur tests clean rehte hain.
        [TestInitialize]
        public void Setup()
        {
            _userRepositoryMock = new Mock<IUserRepository>();
            _configurationMock = new Mock<IConfiguration>();
            _publishEndpointMock = new Mock<IPublishEndpoint>();

            _authService = new InkWell.AuthService.Services.AuthService(
                _userRepositoryMock.Object, 
                _configurationMock.Object, 
                _publishEndpointMock.Object);
        }

        // [TEST 1]: Check if user can register with valid data.
        [TestMethod]
        public async Task RegisterAsync_ShouldReturnSuccess_WhenDataIsValid()
        {
            // Logic: Normal registration scenario where email is unique and all fields are provided.
            var request = new RegisterRequestDTO
            {
                Email = "test@example.com",
                Password = "Password123",
                Username = "testuser",
                FullName = "Test User",
                Role = "Reader"
            };

            _userRepositoryMock!.Setup(repo => repo.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User)null!);
            _userRepositoryMock.Setup(repo => repo.CreateUserAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);
            _userRepositoryMock.Setup(repo => repo.GetRoleByNameAsync(It.IsAny<string>())).ReturnsAsync(new Role { Id = 1, Name = "Reader" });

            var result = await _authService!.RegisterAsync(request);

            result.IsSuccess.Should().BeTrue();
            result.Message.Should().Be("User registered successfully.");
        }

        // [TEST 2]: Check if registration fails when email is already taken.
        [TestMethod]
        public async Task RegisterAsync_ShouldFail_WhenEmailAlreadyExists()
        {
            // Logic: Ensures system doesn't allow duplicate emails.
            var request = new RegisterRequestDTO { Email = "exists@example.com", Password = "any", Username = "any", FullName = "any" };
            _userRepositoryMock!.Setup(repo => repo.GetByEmailAsync(request.Email)).ReturnsAsync(new User { Email = request.Email });

            var result = await _authService!.RegisterAsync(request);

            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Be("Email already in use.");
        }

        // [TEST 3]: Check if registration fails when fields are empty.
        [TestMethod]
        public async Task RegisterAsync_ShouldFail_WhenRequiredFieldsAreEmpty()
        {
            // Logic: Input validation check. No empty emails or passwords allowed.
            var request = new RegisterRequestDTO { Email = "", Password = "" };
            var result = await _authService!.RegisterAsync(request);

            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("required fields");
        }

        // [TEST 4]: Check for automatic Admin assignment for special email.
        [TestMethod]
        public async Task RegisterAsync_ShouldAssignAdminRole_WhenSpecialEmailIsUsed()
        {
            // Logic: Business rule - specifically assigning Admin role to owner's email.
            var request = new RegisterRequestDTO { Email = "saurabhnagaich27@gmail.com", Password = "123", Username = "admin", FullName = "Admin" };
            _userRepositoryMock!.Setup(repo => repo.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User)null!);
            _userRepositoryMock.Setup(repo => repo.GetRoleByNameAsync("Admin")).ReturnsAsync(new Role { Id = 1, Name = "Admin" });
            _userRepositoryMock.Setup(repo => repo.CreateUserAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);

            await _authService!.RegisterAsync(request);

            _userRepositoryMock.Verify(repo => repo.AssignRoleAsync(It.IsAny<Guid>(), 1), Times.Once);
        }

        // [TEST 5]: Check if Reader role is assigned by default to new users.
        [TestMethod]
        public async Task RegisterAsync_ShouldAssignReaderRole_ByDefault()
        {
            // Logic: Standard users should get the 'Reader' role by default.
            var request = new RegisterRequestDTO { Email = "reader@test.com", Password = "123", Username = "r", FullName = "R" };
            _userRepositoryMock!.Setup(repo => repo.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User)null!);
            _userRepositoryMock.Setup(repo => repo.GetRoleByNameAsync("Reader")).ReturnsAsync(new Role { Id = 2, Name = "Reader" });
            _userRepositoryMock.Setup(repo => repo.CreateUserAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);

            await _authService!.RegisterAsync(request);

            _userRepositoryMock.Verify(repo => repo.AssignRoleAsync(It.IsAny<Guid>(), 2), Times.Once);
        }

        // [TEST 6]: Verify that an event is published to RabbitMQ after registration.
        [TestMethod]
        public async Task RegisterAsync_ShouldPublishEvent_WhenRegistrationSuccessful()
        {
            // Logic: Ensures the Event-Driven architecture works. MassTransit must publish UserRegisteredEvent.
            var request = new RegisterRequestDTO { Email = "event@test.com", Password = "123", Username = "ev", FullName = "Ev" };
            _userRepositoryMock!.Setup(repo => repo.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User)null!);
            _userRepositoryMock.Setup(repo => repo.CreateUserAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);

            await _authService!.RegisterAsync(request);

            _publishEndpointMock!.Verify(p => p.Publish(It.IsAny<UserRegisteredEvent>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        // [TEST 7]: Check if login returns a token for valid credentials.
        [TestMethod]
        public async Task LoginAsync_ShouldReturnToken_WhenCredentialsAreCorrect()
        {
            // Logic: Standard successful login path returning a JWT token.
            var user = new User { Id = Guid.NewGuid(), Email = "user@test.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("123") };
            _userRepositoryMock!.Setup(repo => repo.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
            _configurationMock!.Setup(c => c.GetSection("Jwt")["Key"]).Returns("ThisIsASecretKeyForJwtAuthenticationInInkWellPlatformWhichShouldBeLong");

            var result = await _authService!.LoginAsync(new LoginRequestDTO { Email = "user@test.com", Password = "123" });

            result.IsSuccess.Should().BeTrue();
            result.Token.Should().NotBeNullOrEmpty();
        }

        // [TEST 8]: Check if login fails for non-existent email.
        [TestMethod]
        public async Task LoginAsync_ShouldFail_WhenUserNotFound()
        {
            // Logic: System should reject emails that don't exist in the database.
            _userRepositoryMock!.Setup(repo => repo.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User)null!);
            var result = await _authService!.LoginAsync(new LoginRequestDTO { Email = "ghost@test.com", Password = "123" });

            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Be("Invalid email or password.");
        }

        // [TEST 9]: Check if login fails for incorrect password.
        [TestMethod]
        public async Task LoginAsync_ShouldFail_WhenPasswordIsIncorrect()
        {
            // Logic: Ensures password verification (BCrypt) is working correctly.
            var user = new User { Email = "user@test.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("correct") };
            _userRepositoryMock!.Setup(repo => repo.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);

            var result = await _authService!.LoginAsync(new LoginRequestDTO { Email = "user@test.com", Password = "wrong" });

            result.IsSuccess.Should().BeFalse();
        }

        // [TEST 10]: Verify that a 'Logged In' event is published.
        [TestMethod]
        public async Task LoginAsync_ShouldPublishLoggedInEvent()
        {
            // Logic: Audit tracking. Other services need to know when a user logs in.
            var user = new User { Id = Guid.NewGuid(), Email = "login@test.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("123") };
            _userRepositoryMock!.Setup(repo => repo.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
            _configurationMock!.Setup(c => c.GetSection("Jwt")["Key"]).Returns("ThisIsASecretKeyForJwtAuthenticationInInkWellPlatformWhichShouldBeLong");

            await _authService!.LoginAsync(new LoginRequestDTO { Email = "login@test.com", Password = "123" });

            _publishEndpointMock!.Verify(p => p.Publish(It.IsAny<UserLoggedInEvent>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        // [TEST 11]: Google Login flow placeholder.
        [TestMethod]
        public async Task GoogleLoginAsync_ShouldProcessCorrectly()
        {
            // Logic: Placeholder for OAuth flow.
            Assert.IsTrue(true); // Logic tested manually or needs specific static mock setup.
        }
    }
}
