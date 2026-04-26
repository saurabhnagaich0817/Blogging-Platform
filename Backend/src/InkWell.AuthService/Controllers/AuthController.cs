using InkWell.AuthService.DTOs;
using InkWell.AuthService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace InkWell.AuthService.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        /// <summary>
        /// Registers a new user account with the platform.
        /// </summary>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDTO request)
        {
            var response = await _authService.RegisterAsync(request);
            if (!response.IsSuccess)
            {
                _logger.LogWarning("Registration failed for email {Email}: {Message}", request.Email, response.Message);
                return BadRequest(new InkWell.Shared.Responses.BaseResponse<string>(false, response.Message ?? "Registration failed", null));
            }
            
            _logger.LogInformation("User registered successfully with email {Email}", request.Email);
            return Ok(new InkWell.Shared.Responses.BaseResponse<AuthResponseDTO>(true, "User registered successfully", response));
        }

        /// <summary>
        /// Authenticates a user and returns a JWT access token.
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDTO request)
        {
            var response = await _authService.LoginAsync(request);
            if (!response.IsSuccess)
            {
                _logger.LogWarning("Login failed for email {Email}: {Message}", request.Email, response.Message);
                return Unauthorized(new InkWell.Shared.Responses.BaseResponse<string>(false, response.Message ?? "Login failed", null));
            }
            
            _logger.LogInformation("User logged in successfully with email {Email}", request.Email);
            return Ok(new InkWell.Shared.Responses.BaseResponse<AuthResponseDTO>(true, "Login successful", response));
        }

        [HttpPost("google-login")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequestDTO request)
        {
            var response = await _authService.GoogleLoginAsync(request.Token);
            if (!response.IsSuccess)
            {
                _logger.LogWarning("Google login failed: {Message}", response.Message);
                return Unauthorized(new InkWell.Shared.Responses.BaseResponse<string>(false, response.Message ?? "Google login failed", null));
            }

            _logger.LogInformation("User logged in via Google successfully");
            return Ok(new InkWell.Shared.Responses.BaseResponse<AuthResponseDTO>(true, "Google Login successful", response));
        }
    }
}
