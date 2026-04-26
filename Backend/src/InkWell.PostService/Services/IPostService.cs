using InkWell.PostService.DTOs;

namespace InkWell.PostService.Services
{
    public interface IPostService
    {
        Task<PostResponseDTO> CreatePostAsync(CreatePostDTO dto, Guid authorId);
        Task<PostResponseDTO?> GetPostByIdAsync(Guid id);
        Task<IEnumerable<PostResponseDTO>> GetAllPostsAsync();
        Task<PostResponseDTO?> UpdatePostAsync(Guid id, UpdatePostDTO dto, Guid authorId, string role);
        Task<bool> DeletePostAsync(Guid id, Guid authorId, string role);
        Task<int> ToggleLikeAsync(Guid postId, Guid userId, string likerName);
        Task<bool> ToggleSaveAsync(Guid postId, Guid userId);
        Task<IEnumerable<PostResponseDTO>> GetSavedPostsAsync(Guid userId);
        Task<IEnumerable<PostResponseDTO>> GetMyPostsAsync(Guid authorId);
        Task<PostResponseDTO?> GetPostBySlugAsync(string slug);
        Task<AnalyticsResponseDTO> GetAnalyticsAsync(Guid? authorId = null);
    }
}
