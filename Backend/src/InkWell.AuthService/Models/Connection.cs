using System.ComponentModel.DataAnnotations;

namespace InkWell.AuthService.Models
{
    public class Connection
    {
        [Key]
        public Guid Id { get; set; }
        public Guid RequesterId { get; set; }
        public Guid ReceiverId { get; set; }
        public string Status { get; set; } = "Pending"; // Pending, Accepted, Rejected
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public User Requester { get; set; } = null!;
        public User Receiver { get; set; } = null!;
    }
}
