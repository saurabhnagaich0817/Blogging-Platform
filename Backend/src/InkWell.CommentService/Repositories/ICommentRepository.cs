using InkWell.CommentService.Models;

namespace InkWell.CommentService.Repositories
{
    public interface ICommentRepository
    {
        Task<Comment> AddCommentAsync(Comment comment);
        Task<Comment?> GetCommentByIdAsync(Guid id);
        Task<IEnumerable<Comment>> GetCommentsByPostAsync(Guid postId);
        Task<IEnumerable<Comment>> GetRepliesAsync(Guid parentCommentId);
        Task<Comment> UpdateCommentAsync(Comment comment);
        Task DeleteCommentAsync(Comment comment);
    }
}
