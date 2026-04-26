using InkWell.CategoryService.DTOs;
using InkWell.CategoryService.Models;
using InkWell.CategoryService.Repositories;

namespace InkWell.CategoryService.Services
{
    /// <summary>
    /// Service responsible for organizing content through categories and tags (taxonomies).
    /// </summary>
    public class CategoryService : ICategoryService
    {
        private readonly ICategoryRepository _repository;

        public CategoryService(ICategoryRepository repository) { _repository = repository; }

        /// <summary>
        /// Creates a new content category with an automated unique slug.
        /// </summary>
        public async Task<CategoryResponseDTO> CreateCategoryAsync(CreateCategoryDTO dto)
        {
            var slug = await GenerateCategorySlug(dto.Name);
            var category = new Category
            {
                CategoryId = Guid.NewGuid(),
                Name = dto.Name,
                Slug = slug,
                Description = dto.Description ?? "",
                ParentCategoryId = dto.ParentCategoryId,
                CreatedAt = DateTime.UtcNow
            };

            try
            {
                await _repository.CreateCategoryAsync(category);
            }
            catch (Exception)
            {
                category.Slug = $"{slug}-{Guid.NewGuid().ToString().Substring(0, 4)}";
                await _repository.CreateCategoryAsync(category);
            }

            return MapCategoryToDTO(category);
        }

        public async Task<IEnumerable<CategoryResponseDTO>> GetAllCategoriesAsync()
        {
            var categories = await _repository.GetAllCategoriesAsync();
            return categories.Select(MapCategoryToDTO);
        }

        public async Task<CategoryResponseDTO?> GetCategoryBySlugAsync(string slug)
        {
            var category = await _repository.GetCategoryBySlugAsync(slug);
            return category == null ? null : MapCategoryToDTO(category);
        }

        public async Task<bool> DeleteCategoryAsync(Guid id)
        {
            // For simplicity, we are deleting by matching ID after getting all. 
            // Better to add GetCategoryById in repo, but we can do this:
            var cats = await _repository.GetAllCategoriesAsync();
            var category = cats.FirstOrDefault(c => c.CategoryId == id);
            if (category == null) return false;
            
            await _repository.DeleteCategoryAsync(category);
            return true;
        }

        public async Task<TagResponseDTO> CreateTagAsync(CreateTagDTO dto)
        {
            var slug = await GenerateTagSlug(dto.Name);
            var tag = new Tag
            {
                TagId = Guid.NewGuid(),
                Name = dto.Name,
                Slug = slug,
                CreatedAt = DateTime.UtcNow
            };

            try 
            {
                await _repository.CreateTagAsync(tag);
            }
            catch (Exception)
            {
                // Fallback: If slug collision still happens (race condition), force a unique slug
                tag.Slug = $"{slug}-{Guid.NewGuid().ToString().Substring(0, 4)}";
                await _repository.CreateTagAsync(tag);
            }

            return MapTagToDTO(tag);
        }

        public async Task<IEnumerable<TagResponseDTO>> GetAllTagsAsync()
        {
            var tags = await _repository.GetAllTagsAsync();
            return tags.Select(MapTagToDTO);
        }

        public async Task<bool> DeleteTagAsync(Guid id)
        {
            var tag = await _repository.GetTagByIdAsync(id);
            if (tag == null) return false;
            await _repository.DeleteTagAsync(tag);
            return true;
        }

        public async Task<bool> AddTagToPostAsync(Guid postId, Guid tagId)
        {
            var tag = await _repository.GetTagByIdAsync(tagId);
            if (tag == null) return false;

            var existing = await _repository.GetPostTagAsync(postId, tagId);
            if (existing != null) return true; // Already assigned

            await _repository.AddTagToPostAsync(new PostTag
            {
                Id = Guid.NewGuid(),
                PostId = postId,
                TagId = tagId
            });

            // Increment PostCount
            tag.PostCount++;
            await _repository.UpdateTagAsync(tag);
            return true;
        }

        public async Task<bool> RemoveTagFromPostAsync(Guid postId, Guid tagId)
        {
            var pt = await _repository.GetPostTagAsync(postId, tagId);
            if (pt == null) return false;

            await _repository.RemoveTagFromPostAsync(pt);

            var tag = await _repository.GetTagByIdAsync(tagId);
            if (tag != null && tag.PostCount > 0)
            {
                tag.PostCount--;
                await _repository.UpdateTagAsync(tag);
            }
            return true;
        }

        public async Task<IEnumerable<TagResponseDTO>> GetTagsByPostAsync(Guid postId)
        {
            var tags = await _repository.GetTagsByPostAsync(postId);
            return tags.Select(MapTagToDTO);
        }

        public async Task<IEnumerable<TagResponseDTO>> GetTrendingTagsAsync(int count)
        {
            var tags = await _repository.GetTrendingTagsAsync(count);
            return tags.Select(MapTagToDTO);
        }

        private async Task<string> GenerateCategorySlug(string name)
        {
            var slug = name.ToLower().Replace(" ", "-");
            slug = new string(slug.Where(c => char.IsLetterOrDigit(c) || c == '-').ToArray());
            int count = 1;
            string originalSlug = slug;
            while (await _repository.CategorySlugExistsAsync(slug)) { slug = $"{originalSlug}-{count++}"; }
            return slug;
        }

        private async Task<string> GenerateTagSlug(string name)
        {
            var slug = name.ToLower().Replace(" ", "-");
            slug = new string(slug.Where(c => char.IsLetterOrDigit(c) || c == '-').ToArray());
            int count = 1;
            string originalSlug = slug;
            while (await _repository.TagSlugExistsAsync(slug)) { slug = $"{originalSlug}-{count++}"; }
            return slug;
        }

        private CategoryResponseDTO MapCategoryToDTO(Category c) => new CategoryResponseDTO {
            CategoryId = c.CategoryId, Name = c.Name, Slug = c.Slug, Description = c.Description,
            ParentCategoryId = c.ParentCategoryId, PostCount = c.PostCount, CreatedAt = c.CreatedAt
        };

        private TagResponseDTO MapTagToDTO(Tag t) => new TagResponseDTO {
            TagId = t.TagId, Name = t.Name, Slug = t.Slug, PostCount = t.PostCount, CreatedAt = t.CreatedAt
        };
    }
}
