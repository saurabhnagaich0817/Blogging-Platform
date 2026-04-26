using InkWell.CommentService.DTOs;
using InkWell.CommentService.Models;
using InkWell.CommentService.Repositories;
using MassTransit;
using InkWell.Shared.Events;

namespace InkWell.CommentService.Services
{
    /// <summary>
    /// Service responsible for managing user discussions, comments, and replies on stories.
    /// </summary>
    public class CommentService : ICommentService
    {
        private readonly ICommentRepository _repository;
        private readonly IPublishEndpoint _publishEndpoint;

        public CommentService(ICommentRepository repository, IPublishEndpoint publishEndpoint)
        {
            _repository = repository;
            _publishEndpoint = publishEndpoint;
        }

        /// <summary>
        /// Adds a new comment to a post and notifies the post author via an asynchronous event.
        /// </summary>
        public async Task<CommentResponseDTO> AddCommentAsync(CreateCommentDTO dto, Guid authorId, string authorName)
        {
            // Create a new Comment object with the information from the frontend.
            var comment = new Comment
            {
                CommentId = Guid.NewGuid(), // Every comment needs a unique ID.
                PostId = dto.PostId,       // Links the comment to a specific story.
                AuthorId = authorId,       // Who wrote the comment?
                AuthorName = authorName,
                ParentCommentId = dto.ParentCommentId, // If this is a reply, we store the ID of the original comment.
                Content = dto.Content.Trim(),
                Status = "Approved",       // By default, comments are visible immediately.
                LikesCount = 0,
                CreatedAt = DateTime.UtcNow // Store the exact time it was posted.
            };

            // Save the comment to our database.
            var createdComment = await _repository.AddCommentAsync(comment);

            // Step 2: If someone else commented on the author's post, we send a notification.
            if (dto.PostAuthorId.HasValue && dto.PostAuthorId.Value != authorId)
            {
                // We publish an event to RabbitMQ. The NotificationService will pick this up
                // and send an email or an in-app alert to the post author.
                await _publishEndpoint.Publish(new CommentAddedEvent
                {
                    CommentId = createdComment.CommentId,
                    PostId = createdComment.PostId,
                    AuthorId = createdComment.AuthorId,
                    PostAuthorId = dto.PostAuthorId.Value,
                    PostAuthorEmail = dto.PostAuthorEmail ?? string.Empty,
                    ContentPreview = createdComment.Content.Length > 50
                        ? createdComment.Content.Substring(0, 50) + "..."
                        : createdComment.Content
                });
            }

            // Convert the database object into a DTO (Data Transfer Object) to send back to the frontend.
            return MapToResponseDTO(createdComment);
        }

        public async Task<IEnumerable<CommentResponseDTO>> GetCommentsByPostAsync(Guid postId)
        {
            var comments = await _repository.GetCommentsByPostAsync(postId);
            return comments.Select(MapToResponseDTO);
        }

        public async Task<IEnumerable<CommentResponseDTO>> GetRepliesAsync(Guid parentCommentId)
        {
            var replies = await _repository.GetRepliesAsync(parentCommentId);
            return replies.Select(MapToResponseDTO);
        }

        public async Task<CommentResponseDTO?> UpdateCommentAsync(Guid id, UpdateCommentDTO dto, Guid authorId)
        {
            var comment = await _repository.GetCommentByIdAsync(id);
            
            // Only owner can edit, and cannot edit deleted comments
            if (comment == null || comment.AuthorId != authorId || comment.Status == "Deleted") return null;

            comment.Content = dto.Content.Trim();
            comment.UpdatedAt = DateTime.UtcNow;

            var updatedComment = await _repository.UpdateCommentAsync(comment);
            return MapToResponseDTO(updatedComment);
        }

        public async Task<bool> DeleteCommentAsync(Guid id, Guid authorId, string userRole)
        {
            var comment = await _repository.GetCommentByIdAsync(id);
            if (comment == null || comment.Status == "Deleted") return false;

            // Only owner or Admin can delete
            if (comment.AuthorId != authorId && userRole != "Admin") return false;

            // Soft delete logic
            comment.Status = "Deleted";
            comment.UpdatedAt = DateTime.UtcNow;

            await _repository.DeleteCommentAsync(comment);
            return true;
        }

        public async Task<bool> ApproveCommentAsync(Guid id)
        {
            var comment = await _repository.GetCommentByIdAsync(id);
            if (comment == null) return false;

            comment.Status = "Approved";
            await _repository.UpdateCommentAsync(comment);
            return true;
        }

        public async Task<bool> RejectCommentAsync(Guid id)
        {
            var comment = await _repository.GetCommentByIdAsync(id);
            if (comment == null) return false;

            comment.Status = "Rejected";
            await _repository.UpdateCommentAsync(comment);
            return true;
        }

        public async Task<bool> LikeCommentAsync(Guid id)
        {
            var comment = await _repository.GetCommentByIdAsync(id);
            if (comment == null || comment.Status == "Deleted") return false;

            comment.LikesCount++;
            await _repository.UpdateCommentAsync(comment);
            return true;
        }

        public async Task<bool> UnlikeCommentAsync(Guid id)
        {
            var comment = await _repository.GetCommentByIdAsync(id);
            if (comment == null || comment.Status == "Deleted" || comment.LikesCount == 0) return false;

            comment.LikesCount--;
            await _repository.UpdateCommentAsync(comment);
            return true;
        }

        private CommentResponseDTO MapToResponseDTO(Comment comment)
        {
            return new CommentResponseDTO
            {
                CommentId = comment.CommentId,
                PostId = comment.PostId,
                AuthorId = comment.AuthorId,
                AuthorName = comment.AuthorName,
                ParentCommentId = comment.ParentCommentId,
                Content = comment.Content,
                LikesCount = comment.LikesCount,
                Status = comment.Status,
                CreatedAt = comment.CreatedAt,
                UpdatedAt = comment.UpdatedAt
            };
        }
    }
}
