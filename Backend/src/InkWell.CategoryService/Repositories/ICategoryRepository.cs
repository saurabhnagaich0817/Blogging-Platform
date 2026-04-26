using InkWell.CategoryService.Models;

namespace InkWell.CategoryService.Repositories
{
    public interface ICategoryRepository
    {
        Task<Category> CreateCategoryAsync(Category category);
        Task<IEnumerable<Category>> GetAllCategoriesAsync();
        Task<Category?> GetCategoryBySlugAsync(string slug);
        Task DeleteCategoryAsync(Category category);
        
        Task<Tag> CreateTagAsync(Tag tag);
        Task<IEnumerable<Tag>> GetAllTagsAsync();
        Task DeleteTagAsync(Tag tag);
        Task<Tag?> GetTagByIdAsync(Guid id);
        Task UpdateTagAsync(Tag tag);
        
        Task AddTagToPostAsync(PostTag postTag);
        Task RemoveTagFromPostAsync(PostTag postTag);
        Task<IEnumerable<Tag>> GetTagsByPostAsync(Guid postId);
        Task<IEnumerable<Tag>> GetTrendingTagsAsync(int count);
        Task<PostTag?> GetPostTagAsync(Guid postId, Guid tagId);
        
        Task<bool> CategorySlugExistsAsync(string slug);
        Task<bool> TagSlugExistsAsync(string slug);
    }
}
