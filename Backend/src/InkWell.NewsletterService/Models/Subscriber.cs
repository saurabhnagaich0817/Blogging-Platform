namespace InkWell.NewsletterService.Models
{
    public class Subscriber
    {
        public Guid SubscriberId { get; set; }
        public string Email { get; set; } = string.Empty;
        public Guid? UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending"; // Pending, Active, Unsubscribed
        public Guid Token { get; set; }
        public DateTime SubscribedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UnsubscribedAt { get; set; }
    }
}
