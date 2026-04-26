using InkWell.NotificationService.Models;
using Microsoft.EntityFrameworkCore;

namespace InkWell.NotificationService.Data
{
    public class NotificationDbContext : DbContext
    {
        public NotificationDbContext(DbContextOptions<NotificationDbContext> options) : base(options) { }

        public DbSet<Notification> Notifications { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Notification>()
                .HasIndex(notification => new { notification.UserId, notification.CreatedAt });
        }
    }
}
