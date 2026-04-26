using InkWell.CategoryService.Models;
using Microsoft.EntityFrameworkCore;

namespace InkWell.CategoryService.Data
{
    public class CategoryDbContext : DbContext
    {
        public CategoryDbContext(DbContextOptions<CategoryDbContext> options) : base(options) { }

        public DbSet<Category> Categories { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<PostTag> PostTags { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Category>().HasIndex(c => c.Slug).IsUnique();
            modelBuilder.Entity<Tag>().HasIndex(t => t.Slug).IsUnique();
            
            // A post cannot have the exact same tag twice
            modelBuilder.Entity<PostTag>().HasIndex(pt => new { pt.PostId, pt.TagId }).IsUnique();

            // Self-referencing hierarchy for Sub-categories
            modelBuilder.Entity<Category>()
                .HasOne<Category>()
                .WithMany(c => c.SubCategories)
                .HasForeignKey(c => c.ParentCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // Seed Initial Categories
            modelBuilder.Entity<Category>().HasData(
                new Category { CategoryId = Guid.Parse("11111111-1111-1111-1111-111111111111"), Name = "Technology", Slug = "technology", CreatedAt = DateTime.UtcNow },
                new Category { CategoryId = Guid.Parse("22222222-2222-2222-2222-222222222222"), Name = "Lifestyle", Slug = "lifestyle", CreatedAt = DateTime.UtcNow },
                new Category { CategoryId = Guid.Parse("33333333-3333-3333-3333-333333333333"), Name = "Business", Slug = "business", CreatedAt = DateTime.UtcNow },
                new Category { CategoryId = Guid.Parse("44444444-4444-4444-4444-444444444444"), Name = "Health", Slug = "health", CreatedAt = DateTime.UtcNow },
                new Category { CategoryId = Guid.Parse("55555555-5555-5555-5555-555555555555"), Name = "Creative", Slug = "creative", CreatedAt = DateTime.UtcNow }
            );
        }
    }
}
