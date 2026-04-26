using InkWell.MediaService.DTOs;

namespace InkWell.MediaService.Services
{
    public interface IMediaService
    {
        Task<MediaResponseDTO> UploadMediaAsync(UploadMediaDTO dto, Guid uploaderId);
        Task<MediaResponseDTO?> GetMediaByIdAsync(Guid id);
        Task<IEnumerable<MediaResponseDTO>> GetMediaByPostAsync(Guid postId);
        Task<IEnumerable<MediaResponseDTO>> GetMediaByUserAsync(Guid userId);
        Task<IEnumerable<MediaResponseDTO>> GetAllMediaAsync();
        Task<bool> DeleteMediaAsync(Guid id, Guid uploaderId, string role);
        Task<MediaResponseDTO?> UpdateAltTextAsync(Guid id, string altText, Guid uploaderId);
    }
}
