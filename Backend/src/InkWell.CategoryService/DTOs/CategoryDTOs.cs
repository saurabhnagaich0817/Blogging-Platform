using System.ComponentModel.DataAnnotations;

namespace InkWell.CategoryService.DTOs
{
    public class CreateCategoryDTO
    {
        [Required] public string? Name { get; set; }
        public string? Description { get; set; }
        public Guid? ParentCategoryId { get; set; }
    }

    public class UpdateCategoryDTO
    {
        [Required] public string? Name { get; set; }
        public string? Description { get; set; }
    }

    public class CategoryResponseDTO
    {
        public Guid CategoryId { get; set; }
        public string? Name { get; set; }
        public string? Slug { get; set; }
        public string? Description { get; set; }
        public Guid? ParentCategoryId { get; set; }
        public int PostCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateTagDTO
    {
        [Required] public string? Name { get; set; }
    }

    public class TagResponseDTO
    {
        public Guid TagId { get; set; }
        public string? Name { get; set; }
        public string? Slug { get; set; }
        public int PostCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
