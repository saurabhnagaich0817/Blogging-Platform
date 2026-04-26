namespace InkWell.PostService.DTOs
{
    public class PostResponseDTO
    {
        public Guid PostId { get; set; }
        public Guid AuthorId { get; set; }
        public string? AuthorName { get; set; }
        public string? Title { get; set; }
        public string? Slug { get; set; }
        public string? Content { get; set; }
        public string? ImageUrl { get; set; }
        public int LikesCount { get; set; }
        public string? Status { get; set; }
        public Guid? CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
