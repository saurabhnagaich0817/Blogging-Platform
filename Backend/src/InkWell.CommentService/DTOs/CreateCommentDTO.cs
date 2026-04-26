using System.ComponentModel.DataAnnotations;

namespace InkWell.CommentService.DTOs
{
    public class CreateCommentDTO
    {
        [Required]
        public Guid PostId { get; set; }
        
        public Guid? ParentCommentId { get; set; } // Null for main comment, GUID for reply
        
        [Required]
        public string Content { get; set; } = string.Empty;

        public Guid? PostAuthorId { get; set; } // Optional for notifications
        public string? PostAuthorEmail { get; set; }
    }
}
