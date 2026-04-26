namespace InkWell.PostService.Models
{
    public class Post
    {
        public Guid PostId { get; set; } // PK
        public Guid AuthorId { get; set; } // FK to Auth Service User
        public string? AuthorName { get; set; }
        public string? AuthorEmail { get; set; }
        public Guid? CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty; // Unique slug for URL
        public string Content { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public int LikesCount { get; set; } = 0;
        public string Status { get; set; } = "Draft"; // Draft / Published / Unpublished
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
