using InkWell.NewsletterService.DTOs;

namespace InkWell.NewsletterService.Services
{
    public interface INewsletterService
    {
        Task<string> SubscribeAsync(SubscribeDTO dto);
        Task<bool> ConfirmSubscriptionAsync(Guid token);
        Task<bool> UnsubscribeAsync(Guid token);
        Task<IEnumerable<SubscriberResponseDTO>> GetAllSubscribersAsync();
        Task<bool> ApproveSubscriberAsync(Guid id);
        Task<bool> RejectSubscriberAsync(Guid id);
    }
}
