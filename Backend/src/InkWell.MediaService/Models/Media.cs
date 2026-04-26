namespace InkWell.MediaService.Models
{
    public class Media
    {
        public Guid MediaId { get; set; }
        public Guid UploaderId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string OriginalName { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string MimeType { get; set; } = string.Empty;
        public long SizeKb { get; set; }
        public string AltText { get; set; } = string.Empty;
        public Guid? LinkedPostId { get; set; }
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
        public bool IsDeleted { get; set; } = false;
    }
}
