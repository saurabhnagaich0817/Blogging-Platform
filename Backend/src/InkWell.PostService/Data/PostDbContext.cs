using InkWell.PostService.Models;
using Microsoft.EntityFrameworkCore;

namespace InkWell.PostService.Data
{
    public class PostDbContext : DbContext
    {
        public PostDbContext(DbContextOptions<PostDbContext> options) : base(options) { }

        public DbSet<Post> Posts { get; set; }
        public DbSet<Like> Likes { get; set; }
        public DbSet<SavedPost> SavedPosts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Post>(entity =>
            {
                entity.Property(post => post.Title)
                    .HasMaxLength(160);

                entity.Property(post => post.Slug)
                    .HasMaxLength(180);

                entity.HasIndex(post => post.Slug)
                    .IsUnique();
            });
        }
    }
}
