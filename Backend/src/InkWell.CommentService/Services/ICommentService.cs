using InkWell.CommentService.DTOs;

namespace InkWell.CommentService.Services
{
    public interface ICommentService
    {
        Task<CommentResponseDTO> AddCommentAsync(CreateCommentDTO dto, Guid authorId, string authorName);
        Task<IEnumerable<CommentResponseDTO>> GetCommentsByPostAsync(Guid postId);
        Task<IEnumerable<CommentResponseDTO>> GetRepliesAsync(Guid parentCommentId);
        Task<CommentResponseDTO?> UpdateCommentAsync(Guid id, UpdateCommentDTO dto, Guid authorId);
        Task<bool> DeleteCommentAsync(Guid id, Guid authorId, string userRole);
        Task<bool> ApproveCommentAsync(Guid id);
        Task<bool> RejectCommentAsync(Guid id);
        Task<bool> LikeCommentAsync(Guid id);
        Task<bool> UnlikeCommentAsync(Guid id);
    }
}
