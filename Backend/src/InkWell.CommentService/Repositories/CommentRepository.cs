using InkWell.CommentService.Data;
using InkWell.CommentService.Models;
using Microsoft.EntityFrameworkCore;

namespace InkWell.CommentService.Repositories
{
    public class CommentRepository : ICommentRepository
    {
        private readonly CommentDbContext _context;

        public CommentRepository(CommentDbContext context)
        {
            _context = context;
        }

        public async Task<Comment> AddCommentAsync(Comment comment)
        {
            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();
            return comment;
        }

        public async Task<Comment?> GetCommentByIdAsync(Guid id)
        {
            return await _context.Comments.FindAsync(id);
        }

        public async Task<IEnumerable<Comment>> GetCommentsByPostAsync(Guid postId)
        {
            // Only fetch top-level comments for the post that are not deleted or rejected
            return await _context.Comments
                .Where(c => c.PostId == postId && c.ParentCommentId == null && c.Status == "Approved")
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Comment>> GetRepliesAsync(Guid parentCommentId)
        {
            // Fetch replies for a specific parent comment
            return await _context.Comments
                .Where(c => c.ParentCommentId == parentCommentId && c.Status == "Approved")
                .OrderBy(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<Comment> UpdateCommentAsync(Comment comment)
        {
            _context.Comments.Update(comment);
            await _context.SaveChangesAsync();
            return comment;
        }

        public async Task DeleteCommentAsync(Comment comment)
        {
            _context.Comments.Update(comment); // We are doing a soft delete by changing status
            await _context.SaveChangesAsync();
        }
    }
}
