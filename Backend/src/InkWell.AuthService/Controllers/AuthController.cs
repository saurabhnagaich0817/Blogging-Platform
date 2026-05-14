using InkWell.AuthService.DTOs;
using InkWell.AuthService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace InkWell.AuthService.Controllers
{
    /// <summary>
    /// Ye controller user ki entry point hai (Login/Register).
    /// Iska kaam hai user ki identity verify karna aur JWT token issue karna.
    /// </summary>
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
        /// Naye user ko InkWell platform par register karne ke liye.
        /// </summary>
        [HttpPost("register")]
        [Swashbuckle.AspNetCore.Annotations.SwaggerOperation(Summary = "Register a new user", Description = "Creates a new user account with the specified role (Admin, Author, Reader).")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDTO request)
        {
            // Business logic service layer mein hai (Validation, Hashing etc.)
            var response = await _authService.RegisterAsync(request);
            
            if (!response.IsSuccess)
            {
                // Agar registration fail hota hai (jaise email already exists), toh warning log karte hain
                _logger.LogWarning("Registration failed for email {Email}: {Message}", request.Email, response.Message);
                return BadRequest(new InkWell.Shared.Responses.BaseResponse<string>(false, response.Message ?? "Registration failed", null));
            }
            
            _logger.LogInformation("User registered successfully with email {Email}", request.Email);
            return Ok(new InkWell.Shared.Responses.BaseResponse<AuthResponseDTO>(true, "User registered successfully", response));
        }

        /// <summary>
        /// Registered user ko login karwane ke liye. 
        /// Kamyabi par ye ek JWT token deta hai jo frontend ko future requests ke liye chahiye hota hai.
        /// </summary>
        [HttpPost("login")]
        [Swashbuckle.AspNetCore.Annotations.SwaggerOperation(Summary = "User Login", Description = "Authenticates user credentials and returns a JWT Bearer token.")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDTO request)
        {
            var response = await _authService.LoginAsync(request);
            
            if (!response.IsSuccess)
            {
                // Unauthorized (401) bhejte hain agar password ya email galat ho
                _logger.LogWarning("Login failed for email {Email}: {Message}", request.Email, response.Message);
                return Unauthorized(new InkWell.Shared.Responses.BaseResponse<string>(false, response.Message ?? "Login failed", null));
            }
            
            _logger.LogInformation("User logged in successfully with email {Email}", request.Email);
            return Ok(new InkWell.Shared.Responses.BaseResponse<AuthResponseDTO>(true, "Login successful", response));
        }

        /// <summary>
        /// Google One Tap login ko handle karne ke liye.
        /// Frontend se Google token aata hai, hum usey verify karke apna JWT issue karte hain.
        /// </summary>
        [HttpPost("google-login")]
        [Swashbuckle.AspNetCore.Annotations.SwaggerOperation(Summary = "Google Login", Description = "Authenticates a user via Google ID Token and issues an InkWell JWT.")]
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
