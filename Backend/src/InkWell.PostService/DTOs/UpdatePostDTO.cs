using System.ComponentModel.DataAnnotations;

namespace InkWell.PostService.DTOs
{
    public class UpdatePostDTO
    {
        [StringLength(160, MinimumLength = 3)]
        public string? Title { get; set; }

        public string? Content { get; set; }

        [StringLength(32)]
        public string? Status { get; set; }

        public string? ImageUrl { get; set; }

        public Guid? CategoryId { get; set; }

        public string? CategoryName { get; set; }
    }
}
