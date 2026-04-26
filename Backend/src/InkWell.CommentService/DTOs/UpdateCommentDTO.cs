using System.ComponentModel.DataAnnotations;

namespace InkWell.CommentService.DTOs
{
    public class UpdateCommentDTO
    {
        [Required]
        [MinLength(1, ErrorMessage = "Content cannot be empty")]
        public string Content { get; set; } = string.Empty;
    }
}
