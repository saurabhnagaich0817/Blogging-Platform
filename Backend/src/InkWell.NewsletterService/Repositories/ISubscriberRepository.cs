using InkWell.NewsletterService.Models;

namespace InkWell.NewsletterService.Repositories
{
    /// <summary>
    /// Interface for managing newsletter subscriber data persistence.
    /// </summary>
    public interface ISubscriberRepository
    {
        /// <summary>
        /// Adds a new subscriber to the database.
        /// </summary>
        Task<Subscriber> AddSubscriberAsync(Subscriber subscriber);

        /// <summary>
        /// Updates an existing subscriber's metadata.
        /// </summary>
        Task UpdateSubscriberAsync(Subscriber subscriber);

        /// <summary>
        /// Retrieves a subscriber by their email address.
        /// </summary>
        Task<Subscriber?> GetSubscriberByEmailAsync(string email);

        /// <summary>
        /// Retrieves a subscriber by their verification token.
        /// </summary>
        Task<Subscriber?> GetSubscriberByTokenAsync(Guid token);

        /// <summary>
        /// Retrieves all active subscribers in the platform.
        /// </summary>
        Task<IEnumerable<Subscriber>> GetAllSubscribersAsync();

        /// <summary>
        /// Retrieves a subscriber by their unique ID.
        /// </summary>
        Task<Subscriber?> GetSubscriberByIdAsync(Guid id);
    }
}
