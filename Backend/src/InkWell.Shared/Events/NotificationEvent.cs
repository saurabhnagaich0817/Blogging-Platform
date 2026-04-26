using System;

namespace InkWell.Shared.Events
{
    /// <summary>
    /// Event broadcasted when a notification needs to be sent to a user.
    /// Consumed by the NotificationService for database persistence and 
    /// real-time delivery.
    /// </summary>
    public class NotificationEvent : BaseEvent
    {
        public Guid UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = "System";
        public string? UserEmail { get; set; }
        public string? ReferenceId { get; set; }
    }
}
