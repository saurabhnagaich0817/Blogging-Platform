using InkWell.CommentService.DTOs;
using InkWell.CommentService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using InkWell.Shared.Responses;
using InkWell.Shared.Extensions;

namespace InkWell.CommentService.Controllers
{
    /// <summary>
    /// Controller for managing post comments, threaded replies, and moderation.
    /// </summary>
    [ApiController]
    [Route("api/comments")]
    [Produces("application/json")]
    public class CommentController : ControllerBase
    {
        private readonly ICommentService _commentService;

        public CommentController(ICommentService commentService)
        {
            _commentService = commentService;
        }

        /// <summary>
        /// Adds a new comment or a threaded reply to a post.
        /// </summary>
        /// <param name="request">Comment details. Include ParentCommentId for replies.</param>
        [HttpPost]
        [Authorize]
        [SwaggerOperation(Summary = "Add a new comment or reply", Description = "Creates a new comment. Support for threading via ParentCommentId.")]
        [SwaggerResponse(201, "Comment created successfully", typeof(BaseResponse<CommentResponseDTO>))]
        public async Task<IActionResult> AddComment([FromBody] CreateCommentDTO request)
        {
            if (!User.TryGetUserId(out var authorId)) return Unauthorized(new BaseResponse<string>(false, "Invalid user identity.", null));
            
            var authorName = User.GetUsername();
            var response = await _commentService.AddCommentAsync(request, authorId, authorName);
            return CreatedAtAction(nameof(GetCommentsByPost), new { postId = response.PostId }, new BaseResponse<CommentResponseDTO>(true, "Comment posted successfully", response));
        }

        /// <summary>
        /// Retrieves top-level comments for a specific post.
        /// </summary>
        [HttpGet("post/{postId}")]
        [SwaggerOperation(Summary = "Get comments for a post", Description = "Fetches approved top-level comments (excluding replies).")]
        public async Task<IActionResult> GetCommentsByPost(Guid postId)
        {
            var comments = await _commentService.GetCommentsByPostAsync(postId);
            return Ok(new BaseResponse<IEnumerable<CommentResponseDTO>>(true, "Comments fetched successfully", comments));
        }

        /// <summary>
        /// Retrieves all replies associated with a specific parent comment.
        /// </summary>
        [HttpGet("replies/{commentId}")]
        [SwaggerOperation(Summary = "Get replies to a comment", Description = "Fetches all approved replies for a specific parent comment.")]
        public async Task<IActionResult> GetReplies(Guid commentId)
        {
            var replies = await _commentService.GetRepliesAsync(commentId);
            return Ok(new BaseResponse<IEnumerable<CommentResponseDTO>>(true, "Replies fetched successfully", replies));
        }

        /// <summary>
        /// Updates the content of an existing comment. Restricted to the comment owner.
        /// </summary>
        [HttpPut("{id}")]
        [Authorize]
        [SwaggerOperation(Summary = "Edit a comment", Description = "Allows the author to modify their comment content.")]
        public async Task<IActionResult> UpdateComment(Guid id, [FromBody] UpdateCommentDTO request)
        {
            if (!User.TryGetUserId(out var authorId)) return Unauthorized(new BaseResponse<string>(false, "Invalid user identity.", null));
            var response = await _commentService.UpdateCommentAsync(id, request, authorId);
            if (response == null) return StatusCode(403, new BaseResponse<string>(false, "Insufficient permissions to edit this comment.", null));
            
            return Ok(new BaseResponse<CommentResponseDTO>(true, "Comment updated successfully", response));
        }

        /// <summary>
        /// Performs a soft delete on a specific comment.
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize]
        [SwaggerOperation(Summary = "Delete a comment", Description = "Marks a comment as deleted. Restricted to owners or admins.")]
        public async Task<IActionResult> DeleteComment(Guid id)
        {
            if (!User.TryGetUserId(out var authorId)) return Unauthorized(new BaseResponse<string>(false, "Invalid user identity.", null));

            var success = await _commentService.DeleteCommentAsync(id, authorId, User.GetUserRole());
            if (!success) return StatusCode(403, new BaseResponse<string>(false, "Insufficient permissions to delete this comment.", null));

            return Ok(new BaseResponse<string>(true, "Comment deleted successfully", null));
        }

        /// <summary>
        /// Approves a flagged or pending comment. Restricted to Authors and Admins.
        /// </summary>
        [HttpPut("{id}/approve")]
        [Authorize(Roles = "Admin,Author")]
        [SwaggerOperation(Summary = "Approve a comment", Description = "Moderation: Marks a comment as approved and visible to public.")]
        public async Task<IActionResult> ApproveComment(Guid id)
        {
            var success = await _commentService.ApproveCommentAsync(id);
            if (!success) return NotFound(new BaseResponse<string>(false, "Comment not found.", null));
            return Ok(new BaseResponse<string>(true, "Comment approved.", null));
        }

        /// <summary>
        /// Rejects a comment for policy violations. Restricted to Authors and Admins.
        /// </summary>
        [HttpPut("{id}/reject")]
        [Authorize(Roles = "Admin,Author")]
        [SwaggerOperation(Summary = "Reject a comment", Description = "Moderation: Rejects a comment and hides it from public view.")]
        public async Task<IActionResult> RejectComment(Guid id)
        {
            var success = await _commentService.RejectCommentAsync(id);
            if (!success) return NotFound(new BaseResponse<string>(false, "Comment not found.", null));
            return Ok(new BaseResponse<string>(true, "Comment rejected.", null));
        }

        /// <summary>
        /// Increments the like counter for a specific comment.
        /// </summary>
        [HttpPut("{id}/like")]
        [Authorize]
        [SwaggerOperation(Summary = "Like a comment", Description = "Increments the engagement count for a comment.")]
        public async Task<IActionResult> LikeComment(Guid id)
        {
            var success = await _commentService.LikeCommentAsync(id);
            if (!success) return NotFound(new BaseResponse<string>(false, "Comment not found or already deleted.", null));
            return Ok(new BaseResponse<string>(true, "Comment liked.", null));
        }

        /// <summary>
        /// Decrements the like counter for a specific comment.
        /// </summary>
        [HttpPut("{id}/unlike")]
        [Authorize]
        [SwaggerOperation(Summary = "Unlike a comment", Description = "Decrements the engagement count for a comment.")]
        public async Task<IActionResult> UnlikeComment(Guid id)
        {
            var success = await _commentService.UnlikeCommentAsync(id);
            if (!success) return NotFound(new BaseResponse<string>(false, "Comment not found or likes already zero.", null));
            return Ok(new BaseResponse<string>(true, "Comment unliked.", null));
        }
    }
}
