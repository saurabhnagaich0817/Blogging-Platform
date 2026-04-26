using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using InkWell.AuthService.DTOs;
using InkWell.AuthService.Models;
using InkWell.AuthService.Repositories;
using Microsoft.IdentityModel.Tokens;
using MassTransit;
using InkWell.Shared.Events;
using InkWell.Shared.Responses;
using Google.Apis.Auth;

namespace InkWell.AuthService.Services
{
    /// <summary>
    /// Service responsible for handling core authentication, user identity management, 
    /// and social features like subscriptions and connections.
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IConfiguration _configuration;
        private readonly IPublishEndpoint _publishEndpoint;

        public AuthService(IUserRepository userRepository, IConfiguration configuration, IPublishEndpoint publishEndpoint)
        {
            _userRepository = userRepository;
            _configuration = configuration;
            _publishEndpoint = publishEndpoint;
        }

        /// <summary>
        /// Registers a new user in the system with standard email/password credentials.
        /// </summary>
        public async Task<AuthResponseDTO> RegisterAsync(RegisterRequestDTO request)
        {
            // First, we check if all the required fields (Email, Password, etc.) are filled.
            if (string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password) ||
                string.IsNullOrWhiteSpace(request.Username) ||
                string.IsNullOrWhiteSpace(request.FullName))
            {
                // If any field is empty, we return a failure message.
                return new AuthResponseDTO { IsSuccess = false, Message = "All required fields must be provided." };
            }

            // Step 1: Check if a user with this email already exists in our database.
            var existingUser = await _userRepository.GetByEmailAsync(request.Email);
            if (existingUser != null)
            {
                // If the user exists, we cannot register them again.
                return new AuthResponseDTO { IsSuccess = false, Message = "Email already in use." };
            }

            // Step 2: We use BCrypt to securely hash the password before saving it.
            // We never save plain text passwords for security reasons.
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            // Step 3: Create a new User object with the data provided.
            var user = new User
            {
                Id = Guid.NewGuid(), // Generate a unique ID for the new user.
                Username = request.Username.Trim(),
                FullName = request.FullName.Trim(),
                Email = request.Email.Trim(),
                PasswordHash = passwordHash // Save the hashed password, not the real one.
            };

            // Step 4: Save the new user record into the database.
            var createdUser = await _userRepository.CreateUserAsync(user);

            // Step 5: Assign a role to the user (e.g., Reader, Admin).
            var roleName = string.IsNullOrWhiteSpace(request.Role) ? "Reader" : request.Role;
            
            // Special logic: If this specific email registers, make them an Admin.
            if (request.Email.Trim().Equals("saurabhnagaich27@gmail.com", StringComparison.OrdinalIgnoreCase))
            {
                roleName = "Admin";
            }
            
            // Fetch the role object from the DB and assign it to the user.
            var roleToAssign = await _userRepository.GetRoleByNameAsync(roleName) 
                              ?? await _userRepository.GetRoleByNameAsync("Reader");

            if (roleToAssign != null)
            {
                await _userRepository.AssignRoleAsync(createdUser.Id, roleToAssign.Id);
            }

            // Step 6: Publish an event to RabbitMQ so other services know a new user registered.
            await _publishEndpoint.Publish(new UserRegisteredEvent
            {
                UserId = createdUser.Id,
                Email = createdUser.Email,
                FullName = createdUser.FullName,
                Role = roleToAssign?.Name ?? "Reader"
            });

            // Return a success response to the frontend.
            return new AuthResponseDTO { IsSuccess = true, IsNewUser = true, Message = "User registered successfully." };
        }

        /// <summary>
        /// Authenticates a user using email and password, returning a JWT token upon success.
        /// </summary>
        public async Task<AuthResponseDTO> LoginAsync(LoginRequestDTO request)
        {
            // First, check if both email and password are provided in the request.
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return new AuthResponseDTO { IsSuccess = false, Message = "Email and password are required." };
            }

            // Step 1: Find the user in the database using their email.
            var user = await _userRepository.GetByEmailAsync(request.Email.Trim());
            if (user == null)
            {
                // If user not found, return an error.
                return new AuthResponseDTO { IsSuccess = false, Message = "Invalid email or password." };
            }

            // Step 2: Verify if the provided password matches the hashed password stored in the DB.
            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
            if (!isPasswordValid)
            {
                // If password doesn't match, return an error.
                return new AuthResponseDTO { IsSuccess = false, Message = "Invalid email or password." };
            }

            // Step 3: If everything is correct, generate a JWT (JSON Web Token) for the user.
            // This token is used to identify the user in future requests.
            var token = GenerateJwtToken(user);

            // Step 4: Publish a 'Logged In' event so other services can track user activity.
            await _publishEndpoint.Publish(new UserLoggedInEvent
            {
                UserId = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                Role = user.UserRoles?.FirstOrDefault()?.Role?.Name ?? "Reader",
                LoginTime = DateTime.UtcNow
            });

            // Return the success message and the token to the frontend.
            return new AuthResponseDTO
            {
                IsSuccess = true,
                Message = "Login successful.",
                Token = token
            };
        }

        /// <summary>
        /// Handles Google OAuth authentication flow, creating a new user record if it doesn't exist.
        /// </summary>
        public async Task<AuthResponseDTO> GoogleLoginAsync(string token)
        {
            try
            {
                // 1. Validate Google Token 
                // In production, pass ValidationSettings with your ClientId:
                // var settings = new GoogleJsonWebSignature.ValidationSettings { Audience = new[] { "YOUR_CLIENT_ID" } };
                // var payload = await GoogleJsonWebSignature.ValidateAsync(token, settings);
                var payload = await GoogleJsonWebSignature.ValidateAsync(token);

                // 2. Check if user exists, otherwise create
                bool isNew = false;
                var user = await _userRepository.GetByEmailAsync(payload.Email);
                if (user == null)
                {
                    isNew = true;
                    user = new User
                    {
                        Id = Guid.NewGuid(),
                        Username = payload.Email.Split('@')[0] + "_" + Guid.NewGuid().ToString("N").Substring(0,4),
                        FullName = payload.Name ?? "Google User",
                        Email = payload.Email,
                        ProfilePictureUrl = payload.Picture,
                        PasswordHash = "GOOGLE_USER_" + Guid.NewGuid().ToString("N") // Dummy hash
                    };
                    await _userRepository.CreateUserAsync(user);

                    // Google sign-ins default to Reader
                    var roleName = payload.Email == "saurabhnagaich27@gmail.com" ? "Admin" : "Reader";
                    var roleToAssign = await _userRepository.GetRoleByNameAsync(roleName);
                    if (roleToAssign != null) await _userRepository.AssignRoleAsync(user.Id, roleToAssign.Id);
                    
                    // Re-fetch to include roles in token
                    user = await _userRepository.GetByEmailAsync(payload.Email);
                }
                else if (string.IsNullOrEmpty(user.ProfilePictureUrl) && !string.IsNullOrEmpty(payload.Picture))
                {
                    // Update picture if missing
                    user.ProfilePictureUrl = payload.Picture;
                    await _userRepository.UpdateUserAsync(user);
                }

                // 3. Generate JWT
                var jwt = GenerateJwtToken(user!);

                // 4. Publish UserLoggedInEvent
                await _publishEndpoint.Publish(new UserLoggedInEvent
                {
                    UserId = user.Id,
                    Email = user.Email,
                    FullName = user.FullName,
                    Role = user.UserRoles?.FirstOrDefault()?.Role?.Name ?? "Reader",
                    LoginTime = DateTime.UtcNow
                });

                return new AuthResponseDTO
                {
                    IsSuccess = true,
                    IsNewUser = isNew,
                    Message = "Google login successful.",
                    Token = jwt
                };
            }
            catch (Exception ex)
            {
                return new AuthResponseDTO { IsSuccess = false, Message = "Invalid Google token: " + ex.Message };
            }
        }

        public async Task<IEnumerable<UserResponseDTO>> GetAllUsersAsync()
        {
            var users = await _userRepository.GetAllUsersAsync();
            return users.Select(MapToUserResponseDTO);
        }

        public async Task<UserResponseDTO?> GetUserProfileAsync(string username)
        {
            var user = await _userRepository.GetByUsernameAsync(username);
            return user == null ? null : MapToUserResponseDTO(user);
        }

        public async Task<BaseResponse<string>> RequestRoleUpgradeAsync(Guid userId, string requestedRole)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            
            // 1. Notify the User (Requester)
            await _publishEndpoint.Publish(new NotificationEvent
            {
                UserId = userId,
                Title = "Upgrade Request Sent",
                Type = "System",
                Message = $"Your request to upgrade to {requestedRole} has been submitted. Waiting for Admin approval.",
                ReferenceId = null
            });

            // 2. Notify Admins
            await _publishEndpoint.Publish(new NotificationEvent
            {
                UserId = Guid.Empty, // System broadcast for admins
                Title = "New Upgrade Request",
                Type = "System",
                Message = $"User {user?.FullName ?? user?.Username ?? userId.ToString()} requested an upgrade to {requestedRole}.",
                ReferenceId = $"{userId}:{requestedRole}" // Store both ID and Role
            });

            return new BaseResponse<string>(true, "Upgrade request sent successfully.", null);
        }

        public async Task<BaseResponse<string>> ApproveRoleUpgradeAsync(Guid adminId, Guid targetUserId, string targetRole)
        {
            var role = await _userRepository.GetRoleByNameAsync(targetRole);
            if (role == null) return new BaseResponse<string>(false, "Invalid role.", null);

            var user = await _userRepository.GetByIdAsync(targetUserId);
            if (user == null) return new BaseResponse<string>(false, "User not found.", null);
            
            var admin = await _userRepository.GetByIdAsync(adminId);

            await _userRepository.RemoveAllRolesAsync(targetUserId);
            await _userRepository.AssignRoleAsync(targetUserId, role.Id);

            // 1. Notify the user
            await _publishEndpoint.Publish(new NotificationEvent
            {
                UserId = targetUserId,
                Title = "Role Upgraded",
                Type = "System",
                Message = $"Congratulations! Your request to upgrade to {targetRole} has been approved.",
                ReferenceId = null
            });

            // 2. Notify Admins (Success confirmation)
            await _publishEndpoint.Publish(new NotificationEvent
            {
                UserId = Guid.Empty,
                Title = "Upgrade Approved",
                Type = "System",
                Message = $"Admin {admin?.FullName ?? "Someone"} approved {user.FullName}'s upgrade to {targetRole}.",
                ReferenceId = targetUserId.ToString()
            });

            return new BaseResponse<string>(true, "User upgraded successfully.", null);
        }

        public async Task<BaseResponse<string>> UpdateProfilePictureAsync(Guid userId, string url)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) return new BaseResponse<string>(false, "User not found.", null);

            user.ProfilePictureUrl = url;
            await _userRepository.UpdateUserAsync(user);

            return new BaseResponse<string>(true, "Profile picture updated successfully.", url);
        }

        public async Task<BaseResponse<string>> UpdateProfileAsync(Guid userId, UpdateProfileDTO request)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) return new BaseResponse<string>(false, "User not found.", null);

            user.FullName = request.FullName;
            user.Bio = request.Bio;
            user.PhoneNumber = request.PhoneNumber;
            user.LinkedInUrl = request.LinkedInUrl;
            user.GitHubUrl = request.GitHubUrl;

            await _userRepository.UpdateUserAsync(user);

            return new BaseResponse<string>(true, "Profile updated successfully.", null);
        }

        public async Task<BaseResponse<string>> RequestSubscriptionAsync(Guid requesterId, Guid targetUserId)
        {
            if (requesterId == targetUserId) return new BaseResponse<string>(false, "You cannot subscribe to yourself.", null);

            var requester = await _userRepository.GetByIdAsync(requesterId);
            var target = await _userRepository.GetByIdAsync(targetUserId);
            
            if (target == null) return new BaseResponse<string>(false, "User not found.", null);

            // Notify Subscriber
            await _publishEndpoint.Publish(new NotificationEvent
            {
                UserId = requesterId,
                Title = "Subscription Success",
                Type = "Social",
                Message = $"You have subscribed to {target.FullName ?? target.Username}.",
                ReferenceId = targetUserId.ToString()
            });

            // Notify Target User (Author)
            await _publishEndpoint.Publish(new NotificationEvent
            {
                UserId = targetUserId,
                Title = "New Subscriber!",
                Type = "Social",
                Message = $"{requester?.FullName ?? requester?.Username} has subscribed to your stories and newsletter!",
                ReferenceId = requesterId.ToString()
            });

            return new BaseResponse<string>(true, $"You are now subscribed to {target.FullName}.", null);
        }

        public async Task<BaseResponse<string>> RequestConnectionAsync(Guid requesterId, Guid targetUserId)
        {
            if (requesterId == targetUserId) return new BaseResponse<string>(false, "You cannot connect with yourself.", null);

            var existing = await _userRepository.GetConnectionAsync(requesterId, targetUserId);
            if (existing != null) return new BaseResponse<string>(false, "Request already exists or already connected.", null);

            var requester = await _userRepository.GetByIdAsync(requesterId);
            var receiver = await _userRepository.GetByIdAsync(targetUserId);
            
            await _userRepository.CreateConnectionAsync(new Connection
            {
                Id = Guid.NewGuid(),
                RequesterId = requesterId,
                ReceiverId = targetUserId,
                Status = "Pending"
            });

            // 1. Notify the receiver
            await _publishEndpoint.Publish(new NotificationEvent
            {
                UserId = targetUserId,
                Title = "New Connection Request",
                Type = "Social",
                Message = $"{requester?.FullName ?? "Someone"} wants to connect with you.",
                ReferenceId = requesterId.ToString(),
                UserEmail = receiver?.Email ?? string.Empty
            });

            // 2. Notify the requester (Confirmation)
            await _publishEndpoint.Publish(new NotificationEvent
            {
                UserId = requesterId,
                Title = "Connection Request Sent",
                Type = "Social",
                Message = $"Your request to connect with {receiver?.FullName ?? "User"} has been sent.",
                ReferenceId = targetUserId.ToString()
            });

            return new BaseResponse<string>(true, "Connection request sent.", null);
        }

        public async Task<BaseResponse<string>> AcceptConnectionAsync(Guid targetUserId, Guid requesterId)
        {
            var connection = await _userRepository.GetConnectionAsync(requesterId, targetUserId);
            if (connection == null || connection.Status != "Pending") return new BaseResponse<string>(false, "Request not found.", null);

            connection.Status = "Accepted";
            await _userRepository.UpdateConnectionAsync(connection);

            // Notify the requester
            var requester = await _userRepository.GetByIdAsync(requesterId);
            var receiver = await _userRepository.GetByIdAsync(targetUserId);
            await _publishEndpoint.Publish(new NotificationEvent
            {
                UserId = requesterId,
                Title = "Connection Request Accepted",
                Type = "Social",
                Message = $"{receiver?.FullName ?? "User"} accepted your connection request.",
                ReferenceId = targetUserId.ToString(),
                UserEmail = requester?.Email ?? string.Empty
            });

            return new BaseResponse<string>(true, "Connection accepted.", null);
        }

        public async Task<BaseResponse<string>> RejectConnectionAsync(Guid targetUserId, Guid requesterId)
        {
            var connection = await _userRepository.GetConnectionAsync(requesterId, targetUserId);
            if (connection == null) return new BaseResponse<string>(false, "Request not found.", null);

            connection.Status = "Rejected";
            await _userRepository.UpdateConnectionAsync(connection);

            return new BaseResponse<string>(true, "Connection request rejected.", null);
        }

        public async Task<BaseResponse<string>> GetConnectionStatusAsync(Guid userId, Guid targetUserId)
        {
            if (userId == targetUserId) return new BaseResponse<string>(true, "Self", "Self");

            var conn = await _userRepository.GetConnectionAsync(userId, targetUserId);
            if (conn == null) return new BaseResponse<string>(true, "None", "None");

            if (conn.Status == "Accepted") return new BaseResponse<string>(true, "Connected", "Connected");
            
            if (conn.Status == "Pending")
            {
                return conn.RequesterId == userId 
                    ? new BaseResponse<string>(true, "Requested", "Requested") 
                    : new BaseResponse<string>(true, "Inbound", "Inbound");
            }

            return new BaseResponse<string>(true, "None", "None");
        }

        public async Task<IEnumerable<UserResponseDTO>> GetPendingConnectionsAsync(Guid userId)
        {
            var connections = await _userRepository.GetUserConnectionsAsync(userId);
            
            // Only requests where the current user is the RECEIVER and status is Pending
            var pendingRequesterIds = connections
                .Where(c => c.ReceiverId == userId && c.Status == "Pending")
                .Select(c => c.RequesterId)
                .ToList();

            var users = new List<UserResponseDTO>();
            foreach (var id in pendingRequesterIds)
            {
                var user = await _userRepository.GetByIdAsync(id);
                if (user != null)
                {
                    users.Add(MapToUserResponseDTO(user));
                }
            }
            return users;
        }

        public async Task<BaseResponse<string>> DeleteUserAsync(Guid userId)
        {
            await _userRepository.DeleteUserAsync(userId);
            return new BaseResponse<string>(true, "User deleted successfully.", null);
        }

        private UserResponseDTO MapToUserResponseDTO(User user)
        {
            return new UserResponseDTO
            {
                UserId = user.Id,
                Username = user.Username ?? user.Email?.Split('@')[0] ?? string.Empty,
                Email = user.Email,
                DisplayName = user.FullName,
                Bio = user.Bio ?? "Writer at InkWell", 
                ProfilePictureUrl = user.ProfilePictureUrl ?? "assets/images/default-avatar.png",
                PhoneNumber = user.PhoneNumber,
                LinkedInUrl = user.LinkedInUrl,
                GitHubUrl = user.GitHubUrl,
                Role = user.UserRoles?.FirstOrDefault()?.Role?.Name ?? "Reader",
                CreatedAt = user.CreatedAt
            };
        }

        private string GenerateJwtToken(User user)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var key = Encoding.ASCII.GetBytes(jwtSettings["Key"]!);

            // Create claims (payload data in JWT)
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim("userId", user.Id.ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? ""),
                new Claim(ClaimTypes.Name, user.FullName ?? ""),
                new Claim("username", user.Username ?? ""),
                new Claim("profilePictureUrl", user.ProfilePictureUrl ?? "")
            };

            // Add role claims (Defensive check)
            if (user.UserRoles != null)
            {
                foreach (var userRole in user.UserRoles)
                {
                    if (userRole.Role != null)
                    {
                        claims.Add(new Claim(ClaimTypes.Role, userRole.Role.Name));
                        claims.Add(new Claim("role", userRole.Role.Name));
                    }
                }
            }

            // Fallback role if none assigned
            if (!claims.Any(c => c.Type == ClaimTypes.Role))
            {
                claims.Add(new Claim(ClaimTypes.Role, "Reader"));
                claims.Add(new Claim("role", "Reader"));
            }

            // Define token properties
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(int.TryParse(jwtSettings["ExpireHours"], out var expireHours) ? expireHours : 24),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                Issuer = jwtSettings["Issuer"],
                Audience = jwtSettings["Audience"]
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }
    }
}
