using System.Security.Claims;

namespace InkWell.Shared.Extensions
{
    /// <summary>
    /// Extensions for ClaimsPrincipal to simplify user identity extraction.
    /// </summary>
    public static class ClaimsPrincipalExtensions
    {
        /// <summary>
        /// Tries to extract the current user ID from the claims principal.
        /// Checks for NameIdentifier, custom 'userId' claim, and 'sub' claim.
        /// </summary>
        public static bool TryGetUserId(this ClaimsPrincipal user, out Guid userId)
        {
            var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? user.FindFirstValue("userId")
                ?? user.FindFirstValue("sub");

            return Guid.TryParse(userIdClaim, out userId);
        }

        /// <summary>
        /// Retrieves the current user role from claims.
        /// Defaults to 'Reader' if no role is found.
        /// </summary>
        public static string GetUserRole(this ClaimsPrincipal user)
        {
            return user.FindFirstValue(ClaimTypes.Role)
                ?? user.FindFirstValue("role")
                ?? "Reader";
        }

        /// <summary>
        /// Retrieves the username from claims.
        /// </summary>
        public static string GetUsername(this ClaimsPrincipal user)
        {
            return user.FindFirstValue("username") 
                ?? user.FindFirstValue(ClaimTypes.Name) 
                ?? "Anonymous";
        }
    }
}
