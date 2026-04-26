using System.ComponentModel.DataAnnotations;

namespace InkWell.NewsletterService.DTOs
{
    public class SubscribeDTO
    {
        [Required]
        [EmailAddress]
        public string? Email { get; set; }
        public string? FullName { get; set; }
        public Guid? UserId { get; set; }
    }

    public class SubscriberResponseDTO
    {
        public Guid SubscriberId { get; set; }
        public string? Email { get; set; }
        public string? FullName { get; set; }
        public string? Status { get; set; }
        public DateTime SubscribedAt { get; set; }
    }
}
