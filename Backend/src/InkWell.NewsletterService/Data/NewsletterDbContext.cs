using InkWell.NewsletterService.Models;
using Microsoft.EntityFrameworkCore;

namespace InkWell.NewsletterService.Data
{
    public class NewsletterDbContext : DbContext
    {
        public NewsletterDbContext(DbContextOptions<NewsletterDbContext> options) : base(options) { }

        public DbSet<Subscriber> Subscribers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Fix for "Unable to cast object of type 'System.String' to type 'System.Guid'"
            // This forces EF to handle Guid-to-String conversion if the database stores IDs as NVARCHAR
            var guidConverter = new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<Guid, string>(
                v => v.ToString(),
                v => Guid.Parse(v));

            modelBuilder.Entity<Subscriber>(entity =>
            {
                entity.HasIndex(s => s.Email).IsUnique();
                
                entity.Property(e => e.SubscriberId).HasConversion(guidConverter);
                
                // Only convert UserId if it's not null (it's Guid?)
                // Actually, let's keep it simple for all Guids
                entity.Property(e => e.Token).HasConversion(guidConverter);
            });
        }
    }
}
