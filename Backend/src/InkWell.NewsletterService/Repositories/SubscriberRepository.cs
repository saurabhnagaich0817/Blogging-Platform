using InkWell.NewsletterService.Data;
using InkWell.NewsletterService.Models;
using Microsoft.EntityFrameworkCore;

namespace InkWell.NewsletterService.Repositories
{
    /// <summary>
    /// Implementation of the subscriber repository using Entity Framework Core.
    /// </summary>
    public class SubscriberRepository : ISubscriberRepository
    {
        private readonly NewsletterDbContext _context;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubscriberRepository"/>.
        /// </summary>
        public SubscriberRepository(NewsletterDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc/>
        public async Task<Subscriber> AddSubscriberAsync(Subscriber subscriber)
        {
            _context.Subscribers.Add(subscriber);
            await _context.SaveChangesAsync();
            return subscriber;
        }

        /// <inheritdoc/>
        public async Task UpdateSubscriberAsync(Subscriber subscriber)
        {
            _context.Subscribers.Update(subscriber);
            await _context.SaveChangesAsync();
        }

        /// <inheritdoc/>
        public async Task<Subscriber?> GetSubscriberByEmailAsync(string email)
        {
            return await _context.Subscribers.FirstOrDefaultAsync(s => s.Email == email);
        }

        /// <inheritdoc/>
        public async Task<Subscriber?> GetSubscriberByTokenAsync(Guid token)
        {
            return await _context.Subscribers.FirstOrDefaultAsync(s => s.Token == token);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<Subscriber>> GetAllSubscribersAsync()
        {
            return await _context.Subscribers.ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<Subscriber?> GetSubscriberByIdAsync(Guid id)
        {
            return await _context.Subscribers.FindAsync(id);
        }
    }
}
