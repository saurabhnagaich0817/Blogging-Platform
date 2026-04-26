using System.ComponentModel.DataAnnotations;

namespace InkWell.PostService.DTOs
{
    public class CreatePostDTO
    {
        [Required]
        [StringLength(255)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;

        public string? ImageUrl { get; set; }

        public string? Status { get; set; } = "Published";
        
        public string? AuthorName { get; set; }
        public string? AuthorEmail { get; set; }

        public Guid? CategoryId { get; set; }
        public string? CategoryName { get; set; }
    }
}
