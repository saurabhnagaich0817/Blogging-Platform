using InkWell.CategoryService.Data;
using InkWell.CategoryService.Models;
using Microsoft.EntityFrameworkCore;

namespace InkWell.CategoryService.Repositories
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly CategoryDbContext _context;

        public CategoryRepository(CategoryDbContext context) { _context = context; }

        public async Task<Category> CreateCategoryAsync(Category category) {
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();
            return category;
        }

        public async Task<IEnumerable<Category>> GetAllCategoriesAsync() {
            return await _context.Categories.ToListAsync();
        }

        public async Task<Category?> GetCategoryBySlugAsync(string slug) {
            return await _context.Categories.FirstOrDefaultAsync(c => c.Slug == slug);
        }

        public async Task DeleteCategoryAsync(Category category) {
            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
        }

        public async Task<Tag> CreateTagAsync(Tag tag) {
            _context.Tags.Add(tag);
            await _context.SaveChangesAsync();
            return tag;
        }

        public async Task<IEnumerable<Tag>> GetAllTagsAsync() {
            return await _context.Tags.ToListAsync();
        }

        public async Task DeleteTagAsync(Tag tag) {
            _context.Tags.Remove(tag);
            await _context.SaveChangesAsync();
        }
        
        public async Task<Tag?> GetTagByIdAsync(Guid id) {
            return await _context.Tags.FindAsync(id);
        }

        public async Task UpdateTagAsync(Tag tag) {
            _context.Tags.Update(tag);
            await _context.SaveChangesAsync();
        }

        public async Task AddTagToPostAsync(PostTag postTag) {
            _context.PostTags.Add(postTag);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveTagFromPostAsync(PostTag postTag) {
            _context.PostTags.Remove(postTag);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<Tag>> GetTagsByPostAsync(Guid postId) {
            return await _context.PostTags
                .Where(pt => pt.PostId == postId)
                .Include(pt => pt.Tag)
                .Select(pt => pt.Tag)
                .ToListAsync();
        }

        public async Task<IEnumerable<Tag>> GetTrendingTagsAsync(int count) {
            // Sort by highest post count
            return await _context.Tags
                .OrderByDescending(t => t.PostCount)
                .Take(count)
                .ToListAsync();
        }
        
        public async Task<PostTag?> GetPostTagAsync(Guid postId, Guid tagId) {
            return await _context.PostTags.FirstOrDefaultAsync(pt => pt.PostId == postId && pt.TagId == tagId);
        }

        public async Task<bool> CategorySlugExistsAsync(string slug) {
            return await _context.Categories.AnyAsync(c => c.Slug == slug);
        }

        public async Task<bool> TagSlugExistsAsync(string slug) {
            return await _context.Tags.AnyAsync(t => t.Slug == slug);
        }
    }
}
