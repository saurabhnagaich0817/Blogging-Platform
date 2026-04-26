using InkWell.CategoryService.DTOs;

namespace InkWell.CategoryService.Services
{
    public interface ICategoryService
    {
        Task<CategoryResponseDTO> CreateCategoryAsync(CreateCategoryDTO dto);
        Task<IEnumerable<CategoryResponseDTO>> GetAllCategoriesAsync();
        Task<CategoryResponseDTO?> GetCategoryBySlugAsync(string slug);
        Task<bool> DeleteCategoryAsync(Guid id);

        Task<TagResponseDTO> CreateTagAsync(CreateTagDTO dto);
        Task<IEnumerable<TagResponseDTO>> GetAllTagsAsync();
        Task<bool> DeleteTagAsync(Guid id);

        Task<bool> AddTagToPostAsync(Guid postId, Guid tagId);
        Task<bool> RemoveTagFromPostAsync(Guid postId, Guid tagId);
        Task<IEnumerable<TagResponseDTO>> GetTagsByPostAsync(Guid postId);
        Task<IEnumerable<TagResponseDTO>> GetTrendingTagsAsync(int count);
    }
}
