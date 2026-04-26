namespace InkWell.Shared.Events
{
    /// <summary>
    /// Event broadcasted when a new story is published.
    /// Used for newsletter broadcasts and global notifications.
    /// </summary>
    public class PostCreatedEvent : BaseEvent
    {
        public Guid PostId { get; set; }
        public Guid AuthorId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
    }
}
