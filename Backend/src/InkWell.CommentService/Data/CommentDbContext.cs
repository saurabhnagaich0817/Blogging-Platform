using InkWell.CommentService.Models;
using Microsoft.EntityFrameworkCore;

namespace InkWell.CommentService.Data
{
    public class CommentDbContext : DbContext
    {
        public CommentDbContext(DbContextOptions<CommentDbContext> options) : base(options) { }

        public DbSet<Comment> Comments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure self-referencing relationship for Parent/Child threading
            modelBuilder.Entity<Comment>()
                .HasOne<Comment>()
                .WithMany(c => c.Replies)
                .HasForeignKey(c => c.ParentCommentId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascading delete of entire thread if parent is softly deleted
        }
    }
}
