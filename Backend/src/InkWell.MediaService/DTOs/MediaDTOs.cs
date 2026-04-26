using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace InkWell.MediaService.DTOs
{
    public class UploadMediaDTO
    {
        [Required(ErrorMessage = "File is required")]
        public IFormFile File { get; set; } = null!;

        public string? AltText { get; set; }
        public Guid? LinkedPostId { get; set; }
    }
    
    public class UpdateAltTextDTO
    {
        [Required]
        public string? AltText { get; set; }
    }

    public class MediaResponseDTO
    {
        public Guid MediaId { get; set; }
        public Guid UploaderId { get; set; }
        public string? OriginalName { get; set; }
        public string? Url { get; set; }
        public string? MimeType { get; set; }
        public long SizeKb { get; set; }
        public string? AltText { get; set; }
        public Guid? LinkedPostId { get; set; }
        public DateTime UploadedAt { get; set; }
    }
}
