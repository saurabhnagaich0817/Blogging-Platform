namespace InkWell.CategoryService.Models
{
    public class Category
    {
        public Guid CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Guid? ParentCategoryId { get; set; }
        public int PostCount { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation for Hierarchy (Nested Categories)
        public ICollection<Category> SubCategories { get; set; } = new List<Category>();
    }

    public class Tag
    {
        public Guid TagId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public int PostCount { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class PostTag
    {
        public Guid Id { get; set; }
        public Guid PostId { get; set; } // Reference to PostService
        public Guid TagId { get; set; }
        public Tag Tag { get; set; } = null!;
    }
}
