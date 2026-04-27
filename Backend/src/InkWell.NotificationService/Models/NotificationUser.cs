using System.ComponentModel.DataAnnotations;

namespace InkWell.NotificationService.Models
{
    public class NotificationUser
    {
        [Key]
        public Guid UserId { get; set; }
        public string? FullName { get; set; }
        public string? Role { get; set; }
        public DateTime LastSeen { get; set; } = DateTime.UtcNow;
    }
}
