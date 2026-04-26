namespace InkWell.PostService.DTOs
{
    public class AnalyticsResponseDTO
    {
        public int TotalStories { get; set; }
        public int TotalEngagement { get; set; }
        public double StorageUsagePercentage { get; set; }
        public IEnumerable<TrendingStoryDTO>? TrendingStories { get; set; }
        public int SixthOccurrencePosition { get; set; } // Specific requirement from user
    }

    public class TrendingStoryDTO
    {
        public Guid PostId { get; set; }
        public string? Title { get; set; }
        public int Likes { get; set; }
        public int Views { get; set; }
    }
}
