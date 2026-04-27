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
            
            // Email should be unique globally
            modelBuilder.Entity<Subscriber>().HasIndex(s => s.Email).IsUnique();
        }
    }
}
