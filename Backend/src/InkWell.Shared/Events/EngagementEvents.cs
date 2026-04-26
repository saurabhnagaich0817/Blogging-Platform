namespace InkWell.Shared.Events
{
    public class PostLikedEvent : BaseEvent
    {
        public Guid PostId { get; set; }
        public Guid UserId { get; set; } // Who liked
        public string LikerName { get; set; } = string.Empty;
        public Guid PostAuthorId { get; set; } // Who to notify
        public string PostAuthorEmail { get; set; } = string.Empty;
    }

    public class UserSubscribedEvent : BaseEvent
    {
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public DateTime SubscribedAt { get; set; } = DateTime.UtcNow;
    }
}
