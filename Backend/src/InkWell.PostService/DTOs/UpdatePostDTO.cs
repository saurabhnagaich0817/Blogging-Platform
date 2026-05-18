using System.ComponentModel.DataAnnotations;

namespace InkWell.PostService.DTOs
{
    /// <summary>
    /// Data transfer object for updating an existing blog post.
    /// All fields are optional to support partial updates.
    /// </summary>
    public class UpdatePostDTO
    {
        /// <summary>
        /// New title for the post. (Max 160 chars)
        /// </summary>
        [StringLength(160)]
        public string? Title { get; set; }

        /// <summary>
        /// Updated HTML content for the post.
        /// </summary>
        public string? Content { get; set; }

        /// <summary>
        /// New status (Draft, Published, Archived).
        /// </summary>
        [StringLength(32)]
        public string? Status { get; set; }

        /// <summary>
        /// Updated cover image URL.
        /// </summary>
        public string? ImageUrl { get; set; }

        /// <summary>
        /// New category ID.
        /// </summary>
        public Guid? CategoryId { get; set; }

        /// <summary>
        /// New category name for display.
        /// </summary>
        public string? CategoryName { get; set; }
    }
}
