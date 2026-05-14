using InkWell.AuthService.DTOs;
using InkWell.AuthService.Services;
using Microsoft.AspNetCore.Mvc;
using InkWell.Shared.Responses;
using Swashbuckle.AspNetCore.Annotations;

using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace InkWell.AuthService.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class UserController : ControllerBase
    {
        private readonly IAuthService _authService;

        public UserController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpGet]
        [SwaggerOperation(Summary = "Get all users", Description = "Returns a list of all registered users.")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _authService.GetAllUsersAsync();
            return Ok(new BaseResponse<IEnumerable<UserResponseDTO>>(true, "Users fetched successfully", users));
        }

        [HttpGet("profile/{username}")]
        [SwaggerOperation(Summary = "Get user profile", Description = "Fetches public profile data for a specific username.")]
        public async Task<IActionResult> GetProfile(string username)
        {
            var user = await _authService.GetUserProfileAsync(username);
            if (user == null) return NotFound(new BaseResponse<string>(false, "User not found", null));
            return Ok(new BaseResponse<UserResponseDTO>(true, "Profile fetched successfully", user));
        }

        [HttpPost("request-upgrade")]
        [Authorize]
        [SwaggerOperation(Summary = "Request role upgrade", Description = "Allows a Reader to request an upgrade to Author role.")]
        public async Task<IActionResult> RequestUpgrade([FromBody] UpgradeRequestDTO request)
        {
            if (!TryGetCurrentUserId(out var userId)) return Unauthorized(new BaseResponse<string>(false, "Invalid user token.", null));
            var response = await _authService.RequestRoleUpgradeAsync(userId, request.Role);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }

        [HttpPost("{targetUserId}/approve-upgrade")]
        [Authorize(Roles = "Admin")]
        [SwaggerOperation(Summary = "Approve role upgrade", Description = "Admin only. Approves a pending role upgrade request.")]
        public async Task<IActionResult> ApproveUpgrade(Guid targetUserId, [FromBody] UpgradeRequestDTO request)
        {
            if (!TryGetCurrentUserId(out var adminId)) return Unauthorized(new BaseResponse<string>(false, "Invalid user token.", null));
            var response = await _authService.ApproveRoleUpgradeAsync(adminId, targetUserId, request.Role);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }

        [HttpPost("{targetUserId}/assign-role")]
        [Authorize(Roles = "Admin")]
        [SwaggerOperation(Summary = "Manually assign role", Description = "Allows Admin to set any role for any user (Upgrade/Downgrade).")]
        public async Task<IActionResult> AssignRole(Guid targetUserId, [FromBody] UpgradeRequestDTO request)
        {
            if (!TryGetCurrentUserId(out var adminId)) return Unauthorized(new BaseResponse<string>(false, "Invalid user token.", null));
            // We use the same logic as ApproveUpgrade but it's for manual management
            var response = await _authService.ApproveRoleUpgradeAsync(adminId, targetUserId, request.Role);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }

        [HttpPatch("profile-picture")]
        [SwaggerOperation(Summary = "Update profile picture", Description = "Updates the profile picture URL for the logged-in user.")]
        public async Task<IActionResult> UpdateProfilePicture([FromBody] UpdateProfilePictureDTO request)
        {
            if (!TryGetCurrentUserId(out var userId)) return Unauthorized(new BaseResponse<string>(false, "Invalid user token.", null));
            var response = await _authService.UpdateProfilePictureAsync(userId, request.Url);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }

        [HttpPatch("profile")]
        [Authorize]
        [SwaggerOperation(Summary = "Update profile", Description = "Updates user profile details (FullName, Bio, etc.).")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDTO request)
        {
            if (!TryGetCurrentUserId(out var userId)) return Unauthorized(new BaseResponse<string>(false, "Invalid user token.", null));
            var response = await _authService.UpdateProfileAsync(userId, request);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }

        [HttpPost("{targetUserId}/subscribe")]
        [Authorize]
        [SwaggerOperation(Summary = "Subscribe to an author", Description = "Subscribes the current user to another user's newsletter and stories.")]
        public async Task<IActionResult> SubscribeToUser(Guid targetUserId)
        {
            if (!TryGetCurrentUserId(out var userId)) return Unauthorized(new BaseResponse<string>(false, "Invalid user token.", null));
            var response = await _authService.RequestSubscriptionAsync(userId, targetUserId);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }

        [HttpPost("{targetUserId}/connect")]
        [Authorize]
        [SwaggerOperation(Summary = "Request connection", Description = "Sends a connection request to another user.")]
        public async Task<IActionResult> RequestConnection(Guid targetUserId)
        {
            if (!TryGetCurrentUserId(out var userId)) return Unauthorized(new BaseResponse<string>(false, "Invalid user token.", null));
            var response = await _authService.RequestConnectionAsync(userId, targetUserId);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }

        [HttpPost("{requesterId}/accept-connect")]
        [Authorize]
        [SwaggerOperation(Summary = "Accept connection", Description = "Accepts a pending connection request.")]
        public async Task<IActionResult> AcceptConnection(Guid requesterId)
        {
            if (!TryGetCurrentUserId(out var userId)) return Unauthorized(new BaseResponse<string>(false, "Invalid user token.", null));
            var response = await _authService.AcceptConnectionAsync(userId, requesterId);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }

        [HttpPost("{requesterId}/reject-connect")]
        [Authorize]
        [SwaggerOperation(Summary = "Reject connection", Description = "Rejects a pending connection request.")]
        public async Task<IActionResult> RejectConnection(Guid requesterId)
        {
            if (!TryGetCurrentUserId(out var userId)) return Unauthorized(new BaseResponse<string>(false, "Invalid user token.", null));
            var response = await _authService.RejectConnectionAsync(userId, requesterId);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }

        [HttpGet("{targetUserId}/connection-status")]
        [Authorize]
        [SwaggerOperation(Summary = "Get connection status", Description = "Checks the relationship status between current user and target user.")]
        public async Task<IActionResult> GetConnectionStatus(Guid targetUserId)
        {
            if (!TryGetCurrentUserId(out var userId)) return Unauthorized(new BaseResponse<string>(false, "Invalid user token.", null));
            var response = await _authService.GetConnectionStatusAsync(userId, targetUserId);
            return Ok(response);
        }
        
        [HttpGet("pending-connections")]
        [Authorize]
        [SwaggerOperation(Summary = "Get pending connection requests", Description = "Returns a list of users who have requested to connect with the current user.")]
        public async Task<IActionResult> GetPendingConnections()
        {
            if (!TryGetCurrentUserId(out var userId)) return Unauthorized(new BaseResponse<string>(false, "Invalid user token.", null));
            var users = await _authService.GetPendingConnectionsAsync(userId);
            return Ok(new BaseResponse<IEnumerable<UserResponseDTO>>(true, "Pending connections fetched", users));
        }

        [HttpDelete("{targetUserId}")]
        [Authorize(Roles = "Admin")]
        [SwaggerOperation(Summary = "Delete user", Description = "Admin only. Permanently deletes a user from the system.")]
        public async Task<IActionResult> DeleteUser(Guid targetUserId)
        {
            var response = await _authService.DeleteUserAsync(targetUserId);
            return Ok(response);
        }

        private bool TryGetCurrentUserId(out Guid userId)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("userId")
                ?? User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);

            return Guid.TryParse(userIdClaim, out userId);
        }
    }
}
