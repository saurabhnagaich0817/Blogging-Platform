using InkWell.NotificationService.Data;
using InkWell.NotificationService.Models;
using InkWell.Shared.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace InkWell.NotificationService.Controllers
{
    [ApiController]
    [Route("api/notifications")]
    [Authorize]
    public class NotificationController : ControllerBase
    {
        private readonly NotificationDbContext _context;

        public NotificationController(NotificationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetNotifications()
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return Unauthorized(new BaseResponse<string>(false, "Invalid or missing user ID in token.", null));
            }

            var role = User.FindFirstValue(ClaimTypes.Role) ?? User.FindFirstValue("role") ?? "Reader";
            
            var query = _context.Notifications.AsQueryable();
            
            // Show notifications that belong to the user OR are Global Broadcasts (Guid.Empty)
            query = query.Where(n => n.UserId == userId || n.UserId == Guid.Empty);

            var notifications = await query
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            Console.WriteLine($"[DEBUG] Fetching notifications for User: {userId}, Role: {role}, Found: {notifications.Count}");

            return Ok(new BaseResponse<IEnumerable<Notification>>(true, "Notifications fetched", notifications));
        }

        [HttpPatch("{id}/read")]
        public async Task<IActionResult> MarkAsRead(Guid id)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return Unauthorized(new BaseResponse<string>(false, "Invalid or missing user ID in token.", null));
            }

            var notification = await _context.Notifications.FindAsync(id);
            if (notification == null)
            {
                return NotFound(new BaseResponse<string>(false, "Notification not found.", null));
            }

            if (notification.UserId != userId) return Forbid();

            notification.IsRead = true;
            await _context.SaveChangesAsync();

            return Ok(new BaseResponse<string>(true, "Notification marked as read", null));
        }

        [HttpPatch("read-all")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return Unauthorized(new BaseResponse<string>(false, "Invalid or missing user ID in token.", null));
            }

            var unread = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var n in unread) n.IsRead = true;
            await _context.SaveChangesAsync();

            return Ok(new BaseResponse<string>(true, "All notifications marked as read", null));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNotification(Guid id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification == null) return NotFound();

            _context.Notifications.Remove(notification);
            await _context.SaveChangesAsync();

            return Ok(new BaseResponse<string>(true, "Notification deleted", null));
        }

        private bool TryGetCurrentUserId(out Guid userId)
        {
            var idString = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);
            return Guid.TryParse(idString, out userId);
        }
    }
}
