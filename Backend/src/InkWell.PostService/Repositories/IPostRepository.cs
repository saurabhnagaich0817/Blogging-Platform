using InkWell.PostService.Models;

namespace InkWell.PostService.Repositories
{
    public interface IPostRepository
    {
        Task<Post> CreatePostAsync(Post post);
        Task<Post?> GetPostByIdAsync(Guid id);
        Task<IEnumerable<Post>> GetAllPostsAsync();
        Task<Post> UpdatePostAsync(Post post);
        Task DeletePostAsync(Post post);
        Task<bool> SlugExistsAsync(string slug);
        Task<Post?> ToggleLikeAsync(Guid postId, Guid userId);
        Task<bool> ToggleSaveAsync(Guid postId, Guid userId);
        Task<IEnumerable<Post>> GetSavedPostsAsync(Guid userId);
        Task<IEnumerable<Post>> GetPostsByAuthorAsync(Guid authorId);
        Task<Post?> GetPostBySlugAsync(string slug);
    }
}
