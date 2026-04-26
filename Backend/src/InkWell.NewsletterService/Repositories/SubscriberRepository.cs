using InkWell.NewsletterService.Data;
using InkWell.NewsletterService.Models;
using Microsoft.EntityFrameworkCore;

namespace InkWell.NewsletterService.Repositories
{
    public class SubscriberRepository : ISubscriberRepository
    {
        private readonly NewsletterDbContext _context;

        public SubscriberRepository(NewsletterDbContext context)
        {
            _context = context;
        }

        public async Task<Subscriber> AddSubscriberAsync(Subscriber subscriber)
        {
            _context.Subscribers.Add(subscriber);
            await _context.SaveChangesAsync();
            return subscriber;
        }

        public async Task UpdateSubscriberAsync(Subscriber subscriber)
        {
            _context.Subscribers.Update(subscriber);
            await _context.SaveChangesAsync();
        }

        public async Task<Subscriber?> GetSubscriberByEmailAsync(string email)
        {
            return await _context.Subscribers.FirstOrDefaultAsync(s => s.Email == email);
        }

        public async Task<Subscriber?> GetSubscriberByTokenAsync(Guid token)
        {
            return await _context.Subscribers.FirstOrDefaultAsync(s => s.Token == token);
        }

        public async Task<IEnumerable<Subscriber>> GetAllSubscribersAsync()
        {
            return await _context.Subscribers.ToListAsync();
        }

        public async Task<Subscriber?> GetSubscriberByIdAsync(Guid id)
        {
            return await _context.Subscribers.FindAsync(id);
        }
    }
}
