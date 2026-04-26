using InkWell.MediaService.Models;

namespace InkWell.MediaService.Repositories
{
    public interface IMediaRepository
    {
        Task<Media> UploadMediaAsync(Media media);
        Task<Media?> GetMediaByIdAsync(Guid id);
        Task<IEnumerable<Media>> GetMediaByPostAsync(Guid postId);
        Task<IEnumerable<Media>> GetMediaByUserAsync(Guid userId);
        Task<IEnumerable<Media>> GetAllMediaAsync();
        Task UpdateMediaAsync(Media media);
    }
}
