namespace InkWell.Shared.Events
{
    public class CommentAddedEvent : BaseEvent
    {
        public Guid CommentId { get; set; }
        public Guid PostId { get; set; }
        public Guid AuthorId { get; set; }
        public Guid PostAuthorId { get; set; } // Added for notifications
        public string PostAuthorEmail { get; set; } = string.Empty;
        public string ContentPreview { get; set; } = string.Empty;
    }
}
