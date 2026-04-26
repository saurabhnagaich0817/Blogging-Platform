using InkWell.NewsletterService.Models;

namespace InkWell.NewsletterService.Repositories
{
    public interface ISubscriberRepository
    {
        Task<Subscriber> AddSubscriberAsync(Subscriber subscriber);
        Task UpdateSubscriberAsync(Subscriber subscriber);
        Task<Subscriber?> GetSubscriberByEmailAsync(string email);
        Task<Subscriber?> GetSubscriberByTokenAsync(Guid token);
        Task<IEnumerable<Subscriber>> GetAllSubscribersAsync();
        Task<Subscriber?> GetSubscriberByIdAsync(Guid id);
    }
}
