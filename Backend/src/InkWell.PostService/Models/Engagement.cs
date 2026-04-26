using System.ComponentModel.DataAnnotations;

namespace InkWell.PostService.Models
{
    public class Like
    {
        [Key]
        public Guid LikeId { get; set; }
        public Guid PostId { get; set; }
        public Guid UserId { get; set; }
        public DateTime LikedAt { get; set; } = DateTime.UtcNow;
    }

    public class SavedPost
    {
        [Key]
        public Guid SavedPostId { get; set; }
        public Guid PostId { get; set; }
        public Guid UserId { get; set; }
        public DateTime SavedAt { get; set; } = DateTime.UtcNow;
    }
}
