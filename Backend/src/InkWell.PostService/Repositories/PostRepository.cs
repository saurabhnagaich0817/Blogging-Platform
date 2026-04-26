using InkWell.PostService.Data;
using InkWell.PostService.Models;
using Microsoft.EntityFrameworkCore;

namespace InkWell.PostService.Repositories
{
    public class PostRepository : IPostRepository
    {
        private readonly PostDbContext _context;

        public PostRepository(PostDbContext context)
        {
            _context = context;
        }

        public async Task<Post> CreatePostAsync(Post post)
        {
            try
            {
                _context.Posts.Add(post);
                await _context.SaveChangesAsync();
                return post;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving post to DB: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
                throw;
            }
        }

        public async Task<Post?> GetPostByIdAsync(Guid id)
        {
            return await _context.Posts.FindAsync(id);
        }

        public async Task<IEnumerable<Post>> GetAllPostsAsync()
        {
            return await _context.Posts.ToListAsync();
        }

        public async Task<Post> UpdatePostAsync(Post post)
        {
            _context.Posts.Update(post);
            await _context.SaveChangesAsync();
            return post;
        }

        public async Task DeletePostAsync(Post post)
        {
            _context.Posts.Remove(post);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> SlugExistsAsync(string slug)
        {
            return await _context.Posts.AnyAsync(p => p.Slug == slug);
        }

        public async Task<Post?> ToggleLikeAsync(Guid postId, Guid userId)
        {
            var existingLike = await _context.Likes.FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userId);
            var post = await _context.Posts.FindAsync(postId);
            if (post == null) return null;

            if (existingLike != null)
            {
                _context.Likes.Remove(existingLike);
                post.LikesCount = Math.Max(0, post.LikesCount - 1);
            }
            else
            {
                _context.Likes.Add(new Like { PostId = postId, UserId = userId });
                post.LikesCount++;
            }

            await _context.SaveChangesAsync();
            return post;
        }

        public async Task<bool> ToggleSaveAsync(Guid postId, Guid userId)
        {
            var existingSave = await _context.SavedPosts.FirstOrDefaultAsync(s => s.PostId == postId && s.UserId == userId);
            bool isSaved = false;

            if (existingSave != null)
            {
                _context.SavedPosts.Remove(existingSave);
            }
            else
            {
                _context.SavedPosts.Add(new SavedPost { PostId = postId, UserId = userId });
                isSaved = true;
            }

            await _context.SaveChangesAsync();
            return isSaved;
        }

        public async Task<IEnumerable<Post>> GetSavedPostsAsync(Guid userId)
        {
            var savedPostIds = await _context.SavedPosts
                .Where(s => s.UserId == userId)
                .Select(s => s.PostId)
                .ToListAsync();

            return await _context.Posts
                .Where(p => savedPostIds.Contains(p.PostId))
                .ToListAsync();
        }

        public async Task<IEnumerable<Post>> GetPostsByAuthorAsync(Guid authorId)
        {
            return await _context.Posts
                .Where(p => p.AuthorId == authorId)
                .ToListAsync();
        }

        public async Task<Post?> GetPostBySlugAsync(string slug)
        {
            return await _context.Posts.FirstOrDefaultAsync(p => p.Slug == slug);
        }
    }
}
