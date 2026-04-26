using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using InkWell.AuthService.DTOs;
using InkWell.Shared.Responses;

namespace InkWell.AuthService.Services
{
    public interface IAuthService
    {
        Task<AuthResponseDTO> RegisterAsync(RegisterRequestDTO request);
        Task<AuthResponseDTO> LoginAsync(LoginRequestDTO request);
        Task<AuthResponseDTO> GoogleLoginAsync(string token);
        Task<IEnumerable<UserResponseDTO>> GetAllUsersAsync();
        Task<UserResponseDTO?> GetUserProfileAsync(string username);
        Task<BaseResponse<string>> RequestRoleUpgradeAsync(Guid userId, string requestedRole);
        Task<BaseResponse<string>> ApproveRoleUpgradeAsync(Guid adminId, Guid targetUserId, string targetRole);
        Task<BaseResponse<string>> UpdateProfilePictureAsync(Guid userId, string url);
        Task<BaseResponse<string>> UpdateProfileAsync(Guid userId, UpdateProfileDTO request);
        Task<BaseResponse<string>> RequestSubscriptionAsync(Guid requesterId, Guid targetUserId);
        
        // Connections
        Task<BaseResponse<string>> RequestConnectionAsync(Guid requesterId, Guid targetUserId);
        Task<BaseResponse<string>> AcceptConnectionAsync(Guid targetUserId, Guid requesterId);
        Task<BaseResponse<string>> RejectConnectionAsync(Guid targetUserId, Guid requesterId);
        Task<BaseResponse<string>> GetConnectionStatusAsync(Guid userId, Guid targetUserId);
        Task<IEnumerable<UserResponseDTO>> GetPendingConnectionsAsync(Guid userId);
        Task<BaseResponse<string>> DeleteUserAsync(Guid userId);
    }
}
