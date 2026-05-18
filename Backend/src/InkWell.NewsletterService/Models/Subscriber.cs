namespace InkWell.NewsletterService.Models
{
    /// <summary>
    /// Represents a newsletter subscriber in the platform.
    /// </summary>
    public class Subscriber
    {
        /// <summary>
        /// Unique identifier for the subscriber.
        /// </summary>
        public Guid SubscriberId { get; set; }

        /// <summary>
        /// The email address of the subscriber.
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Optional link to an authenticated User ID.
        /// </summary>
        public Guid? UserId { get; set; }

        /// <summary>
        /// The full name of the subscriber.
        /// </summary>
        public string FullName { get; set; } = string.Empty;

        /// <summary>
        /// Current status: Pending, Active, or Unsubscribed.
        /// </summary>
        public string Status { get; set; } = "Pending";

        /// <summary>
        /// Verification token for double-opt-in workflows.
        /// </summary>
        public Guid Token { get; set; }

        /// <summary>
        /// Timestamp when the subscription was initiated.
        /// </summary>
        public DateTime SubscribedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Timestamp when the user unsubscribed.
        /// </summary>
        public DateTime? UnsubscribedAt { get; set; }
    }
}
