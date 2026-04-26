using InkWell.MediaService.Models;
using Microsoft.EntityFrameworkCore;

namespace InkWell.MediaService.Data
{
    public class MediaDbContext : DbContext
    {
        public MediaDbContext(DbContextOptions<MediaDbContext> options) : base(options) { }

        public DbSet<Media> MediaItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Global query filter to automatically exclude soft-deleted media items
            modelBuilder.Entity<Media>().HasQueryFilter(m => !m.IsDeleted);
        }
    }
}
