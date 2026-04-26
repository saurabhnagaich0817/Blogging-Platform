namespace InkWell.CommentService.Models
{
    public class Comment
    {
        public Guid CommentId { get; set; } // PK
        public Guid PostId { get; set; } // Reference to PostService
        public Guid AuthorId { get; set; } // Reference to AuthService
        public string? AuthorName { get; set; } // Nullable to handle existing records
        public Guid? ParentCommentId { get; set; } // Null = Top-level, GUID = Reply
        public string Content { get; set; } = string.Empty;
        public int LikesCount { get; set; } = 0;
        public string Status { get; set; } = "Approved"; // Approved, Pending, Rejected, Deleted
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation property for threading (Replies)
        public ICollection<Comment> Replies { get; set; } = new List<Comment>();
    }
}
