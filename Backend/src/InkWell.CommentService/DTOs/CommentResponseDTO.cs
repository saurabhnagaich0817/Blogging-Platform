namespace InkWell.CommentService.DTOs
{
    public class CommentResponseDTO
    {
        public Guid CommentId { get; set; }
        public Guid PostId { get; set; }
        public Guid AuthorId { get; set; }
        public string? AuthorName { get; set; }
        public Guid? ParentCommentId { get; set; }
        public string? Content { get; set; }
        public int LikesCount { get; set; }
        public string? Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
