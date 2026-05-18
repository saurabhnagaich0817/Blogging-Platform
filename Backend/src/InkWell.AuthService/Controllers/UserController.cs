using InkWell.AuthService.DTOs;
using InkWell.AuthService.Services;
using InkWell.Shared.Responses;
using InkWell.Shared.Extensions;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Microsoft.AspNetCore.Authorization;

namespace InkWell.AuthService.Controllers
{
    /// <summary>
    /// Controller for managing user profiles, role upgrades, and social connections.
    /// </summary>
    [ApiController]
    [Route("api/users")]
    [Produces("application/json")]
    public class UserController : ControllerBase
    {
        private readonly IAuthService _authService;

        public UserController(IAuthService authService)
        {
            _authService = authService;
        }

        /// <summary>
        /// Retrieves a list of all registered users in the platform.
        /// </summary>
        [HttpGet]
        [SwaggerOperation(Summary = "Get all users", Description = "Returns a list of all registered users.")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _authService.GetAllUsersAsync();
            return Ok(new BaseResponse<IEnumerable<UserResponseDTO>>(true, "Users fetched successfully", users));
        }

        /// <summary>
        /// Fetches the public profile details for a specific username.
        /// </summary>
        [HttpGet("profile/{username}")]
        [SwaggerOperation(Summary = "Get user profile", Description = "Fetches public profile data (bio, followers, etc.) for a specific username.")]
        public async Task<IActionResult> GetProfile(string username)
        {
            var user = await _authService.GetUserProfileAsync(username);
            if (user == null) return NotFound(new BaseResponse<string>(false, "User profile not found", null));
            return Ok(new BaseResponse<UserResponseDTO>(true, "Profile fetched successfully", user));
        }

        /// <summary>
        /// Initiates a role upgrade request for the currently authenticated user.
        /// </summary>
        [HttpPost("request-upgrade")]
        [Authorize]
        [SwaggerOperation(Summary = "Request role upgrade", Description = "Allows a Reader to request an upgrade to Author or Admin roles.")]
        public async Task<IActionResult> RequestUpgrade([FromBody] UpgradeRequestDTO request)
        {
            if (!User.TryGetUserId(out var userId)) return Unauthorized(new BaseResponse<string>(false, "Invalid or expired user token.", null));
            var response = await _authService.RequestRoleUpgradeAsync(userId, request.Role);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }

        /// <summary>
        /// Approves a pending role upgrade request. Restricted to Administrators.
        /// </summary>
        [HttpPost("{targetUserId}/approve-upgrade")]
        [Authorize(Roles = "Admin")]
        [SwaggerOperation(Summary = "Approve role upgrade", Description = "Admin only. Approves a pending role upgrade request for another user.")]
        public async Task<IActionResult> ApproveUpgrade(Guid targetUserId, [FromBody] UpgradeRequestDTO request)
        {
            if (!User.TryGetUserId(out var adminId)) return Unauthorized(new BaseResponse<string>(false, "Invalid or expired admin token.", null));
            var response = await _authService.ApproveRoleUpgradeAsync(adminId, targetUserId, request.Role);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }

        /// <summary>
        /// Manually assigns a specific role to a user. Restricted to Administrators.
        /// </summary>
        [HttpPost("{targetUserId}/assign-role")]
        [Authorize(Roles = "Admin")]
        [SwaggerOperation(Summary = "Manually assign role", Description = "Allows Admin to bypass request flow and set a specific role for a user.")]
        public async Task<IActionResult> AssignRole(Guid targetUserId, [FromBody] UpgradeRequestDTO request)
        {
            if (!User.TryGetUserId(out var adminId)) return Unauthorized(new BaseResponse<string>(false, "Invalid or expired admin token.", null));
            var response = await _authService.ApproveRoleUpgradeAsync(adminId, targetUserId, request.Role);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }

        /// <summary>
        /// Updates the profile picture URL for the currently authenticated user.
        /// </summary>
        [HttpPatch("profile-picture")]
        [Authorize]
        [SwaggerOperation(Summary = "Update profile picture", Description = "Updates the avatar URL for the logged-in user.")]
        public async Task<IActionResult> UpdateProfilePicture([FromBody] UpdateProfilePictureDTO request)
        {
            if (!User.TryGetUserId(out var userId)) return Unauthorized(new BaseResponse<string>(false, "Invalid or expired user token.", null));
            var response = await _authService.UpdateProfilePictureAsync(userId, request.Url);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }

        /// <summary>
        /// Updates the general profile information (Bio, FullName) for the authenticated user.
        /// </summary>
        [HttpPatch("profile")]
        [Authorize]
        [SwaggerOperation(Summary = "Update profile", Description = "Updates user profile details like FullName, Bio, and social links.")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDTO request)
        {
            if (!User.TryGetUserId(out var userId)) return Unauthorized(new BaseResponse<string>(false, "Invalid or expired user token.", null));
            var response = await _authService.UpdateProfileAsync(userId, request);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }

        /// <summary>
        /// Subscribes the current user to an author's updates and newsletter.
        /// </summary>
        [HttpPost("{targetUserId}/subscribe")]
        [Authorize]
        [SwaggerOperation(Summary = "Subscribe to an author", Description = "Follows an author and opts into their newsletter updates.")]
        public async Task<IActionResult> SubscribeToUser(Guid targetUserId)
        {
            if (!User.TryGetUserId(out var userId)) return Unauthorized(new BaseResponse<string>(false, "Invalid or expired user token.", null));
            var response = await _authService.RequestSubscriptionAsync(userId, targetUserId);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }

        /// <summary>
        /// Sends a connection request to another user.
        /// </summary>
        [HttpPost("{targetUserId}/connect")]
        [Authorize]
        [SwaggerOperation(Summary = "Request connection", Description = "Initiates a connection request between the current user and target user.")]
        public async Task<IActionResult> RequestConnection(Guid targetUserId)
        {
            if (!User.TryGetUserId(out var userId)) return Unauthorized(new BaseResponse<string>(false, "Invalid or expired user token.", null));
            var response = await _authService.RequestConnectionAsync(userId, targetUserId);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }

        /// <summary>
        /// Accepts an incoming connection request.
        /// </summary>
        [HttpPost("{requesterId}/accept-connect")]
        [Authorize]
        [SwaggerOperation(Summary = "Accept connection", Description = "Accepts a pending incoming connection request.")]
        public async Task<IActionResult> AcceptConnection(Guid requesterId)
        {
            if (!User.TryGetUserId(out var userId)) return Unauthorized(new BaseResponse<string>(false, "Invalid or expired user token.", null));
            var response = await _authService.AcceptConnectionAsync(userId, requesterId);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }

        /// <summary>
        /// Rejects an incoming connection request.
        /// </summary>
        [HttpPost("{requesterId}/reject-connect")]
        [Authorize]
        [SwaggerOperation(Summary = "Reject connection", Description = "Rejects and removes a pending incoming connection request.")]
        public async Task<IActionResult> RejectConnection(Guid requesterId)
        {
            if (!User.TryGetUserId(out var userId)) return Unauthorized(new BaseResponse<string>(false, "Invalid or expired user token.", null));
            var response = await _authService.RejectConnectionAsync(userId, requesterId);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }

        /// <summary>
        /// Retrieves the current connection status with another user.
        /// </summary>
        [HttpGet("{targetUserId}/connection-status")]
        [Authorize]
        [SwaggerOperation(Summary = "Get connection status", Description = "Checks whether the users are connected, pending, or strangers.")]
        public async Task<IActionResult> GetConnectionStatus(Guid targetUserId)
        {
            if (!User.TryGetUserId(out var userId)) return Unauthorized(new BaseResponse<string>(false, "Invalid or expired user token.", null));
            var response = await _authService.GetConnectionStatusAsync(userId, targetUserId);
            return Ok(response);
        }
        
        /// <summary>
        /// Retrieves all pending connection requests for the authenticated user.
        /// </summary>
        [HttpGet("pending-connections")]
        [Authorize]
        [SwaggerOperation(Summary = "Get pending connection requests", Description = "Returns a list of users awaiting connection approval from the current user.")]
        public async Task<IActionResult> GetPendingConnections()
        {
            if (!User.TryGetUserId(out var userId)) return Unauthorized(new BaseResponse<string>(false, "Invalid or expired user token.", null));
            var users = await _authService.GetPendingConnectionsAsync(userId);
            return Ok(new BaseResponse<IEnumerable<UserResponseDTO>>(true, "Pending requests fetched successfully", users));
        }

        /// <summary>
        /// Permanently removes a user from the system. Restricted to Administrators.
        /// </summary>
        [HttpDelete("{targetUserId}")]
        [Authorize(Roles = "Admin")]
        [SwaggerOperation(Summary = "Delete user", Description = "Admin only. Permanently deletes a user and associated data.")]
        public async Task<IActionResult> DeleteUser(Guid targetUserId)
        {
            var response = await _authService.DeleteUserAsync(targetUserId);
            return Ok(response);
        }
    }
}
