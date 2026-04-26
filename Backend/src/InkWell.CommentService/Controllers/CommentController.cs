using System.Security.Claims;
using InkWell.CommentService.DTOs;
using InkWell.CommentService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using InkWell.Shared.Responses;

namespace InkWell.CommentService.Controllers
{
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

        [HttpPost]
        [Authorize]
        [SwaggerOperation(
            Summary = "Add a new comment or reply",
            Description = "Creates a new comment on a specific post. Pass ParentCommentId to create a reply to an existing comment. Requires Authorization.",
            OperationId = "AddComment")]
        [SwaggerResponse(201, "Comment created successfully", typeof(CommentResponseDTO))]
        [SwaggerResponse(400, "Invalid input data")]
        [SwaggerResponse(401, "Unauthorized - User must be logged in")]
        public async Task<IActionResult> AddComment([FromBody] CreateCommentDTO request)
        {
            var authorIdString = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);
            if (string.IsNullOrEmpty(authorIdString) || !Guid.TryParse(authorIdString, out Guid authorId)) return Unauthorized(new BaseResponse<string>(false, "Invalid user token.", null));
            
            var authorName = User.FindFirstValue("username") ?? User.FindFirstValue(ClaimTypes.Name) ?? "User";
            var response = await _commentService.AddCommentAsync(request, authorId, authorName);
            return CreatedAtAction(nameof(GetCommentsByPost), new { postId = response.PostId }, new BaseResponse<CommentResponseDTO>(true, "Comment created successfully", response));
        }

        [HttpGet("post/{postId}")]
        [SwaggerOperation(
            Summary = "Get top-level comments for a post",
            Description = "Retrieves all approved top-level comments for a specific post. This does not return replies.",
            OperationId = "GetCommentsByPost")]
        [SwaggerResponse(200, "Successfully retrieved comments", typeof(IEnumerable<CommentResponseDTO>))]
        public async Task<IActionResult> GetCommentsByPost(Guid postId)
        {
            var comments = await _commentService.GetCommentsByPostAsync(postId);
            return Ok(new BaseResponse<IEnumerable<CommentResponseDTO>>(true, "Comments fetched successfully", comments));
        }

        [HttpGet("replies/{commentId}")]
        [SwaggerOperation(
            Summary = "Get replies to a comment",
            Description = "Retrieves all approved replies for a specific parent comment. Use this for recursive threading display.",
            OperationId = "GetReplies")]
        [SwaggerResponse(200, "Successfully retrieved replies", typeof(IEnumerable<CommentResponseDTO>))]
        public async Task<IActionResult> GetReplies(Guid commentId)
        {
            var replies = await _commentService.GetRepliesAsync(commentId);
            return Ok(new BaseResponse<IEnumerable<CommentResponseDTO>>(true, "Replies fetched successfully", replies));
        }

        [HttpPut("{id}")]
        [Authorize]
        [SwaggerOperation(
            Summary = "Edit a comment",
            Description = "Allows the author of the comment to edit its content. Requires Authorization.",
            OperationId = "UpdateComment")]
        [SwaggerResponse(200, "Comment updated successfully", typeof(CommentResponseDTO))]
        [SwaggerResponse(401, "Unauthorized - User must be logged in")]
        [SwaggerResponse(403, "Forbidden - Only the comment owner can edit")]
        public async Task<IActionResult> UpdateComment(Guid id, [FromBody] UpdateCommentDTO request)
        {
            var authorIdString = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);
            if (string.IsNullOrEmpty(authorIdString) || !Guid.TryParse(authorIdString, out Guid authorId)) return Unauthorized(new BaseResponse<string>(false, "Invalid user token.", null));
            var response = await _commentService.UpdateCommentAsync(id, request, authorId);
            if (response == null) return StatusCode(403, new BaseResponse<string>(false, "You do not have permission to edit this comment.", null));
            
            return Ok(new BaseResponse<CommentResponseDTO>(true, "Comment updated successfully", response));
        }

        [HttpDelete("{id}")]
        [Authorize]
        [SwaggerOperation(
            Summary = "Delete a comment (Soft Delete)",
            Description = "Marks a comment as 'Deleted'. Only the author or an Admin can perform this action.",
            OperationId = "DeleteComment")]
        [SwaggerResponse(204, "Comment deleted successfully")]
        [SwaggerResponse(401, "Unauthorized")]
        [SwaggerResponse(403, "Forbidden - Insufficient permissions")]
        public async Task<IActionResult> DeleteComment(Guid id)
        {
            var authorIdString = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);
            if (string.IsNullOrEmpty(authorIdString) || !Guid.TryParse(authorIdString, out Guid authorId)) return Unauthorized(new BaseResponse<string>(false, "Invalid user token.", null));
            var role = GetUserRoleFromToken();

            var success = await _commentService.DeleteCommentAsync(id, authorId, role);
            if (!success) return StatusCode(403, new BaseResponse<string>(false, "You do not have permission to delete this comment.", null));

            return Ok(new BaseResponse<string>(true, "Comment deleted successfully", null));
        }

        [HttpPut("{id}/approve")]
        [Authorize(Roles = "Admin,Author")]
        [SwaggerOperation(
            Summary = "Approve a comment",
            Description = "Moderation logic: Approves a pending/rejected comment. Requires Admin or Author role.",
            OperationId = "ApproveComment")]
        [SwaggerResponse(200, "Comment approved")]
        [SwaggerResponse(401, "Unauthorized")]
        [SwaggerResponse(403, "Forbidden - Insufficient role")]
        [SwaggerResponse(404, "Comment not found")]
        public async Task<IActionResult> ApproveComment(Guid id)
        {
            var success = await _commentService.ApproveCommentAsync(id);
            if (!success) return NotFound(new BaseResponse<string>(false, "Comment not found.", null));
            return Ok(new BaseResponse<string>(true, "Comment approved.", null));
        }

        [HttpPut("{id}/reject")]
        [Authorize(Roles = "Admin,Author")]
        [SwaggerOperation(
            Summary = "Reject a comment",
            Description = "Moderation logic: Rejects an inappropriate comment. Requires Admin or Author role.",
            OperationId = "RejectComment")]
        [SwaggerResponse(200, "Comment rejected")]
        [SwaggerResponse(401, "Unauthorized")]
        [SwaggerResponse(403, "Forbidden - Insufficient role")]
        [SwaggerResponse(404, "Comment not found")]
        public async Task<IActionResult> RejectComment(Guid id)
        {
            var success = await _commentService.RejectCommentAsync(id);
            if (!success) return NotFound(new BaseResponse<string>(false, "Comment not found.", null));
            return Ok(new BaseResponse<string>(true, "Comment rejected.", null));
        }

        [HttpPut("{id}/like")]
        [Authorize]
        [SwaggerOperation(
            Summary = "Like a comment",
            Description = "Increments the likes count of a specific comment. Requires Authorization.",
            OperationId = "LikeComment")]
        [SwaggerResponse(200, "Comment liked")]
        [SwaggerResponse(401, "Unauthorized")]
        [SwaggerResponse(404, "Comment not found or deleted")]
        public async Task<IActionResult> LikeComment(Guid id)
        {
            var success = await _commentService.LikeCommentAsync(id);
            if (!success) return NotFound(new BaseResponse<string>(false, "Comment not found or deleted.", null));
            return Ok(new BaseResponse<string>(true, "Comment liked.", null));
        }

        [HttpPut("{id}/unlike")]
        [Authorize]
        [SwaggerOperation(
            Summary = "Unlike a comment",
            Description = "Decrements the likes count of a specific comment. Requires Authorization.",
            OperationId = "UnlikeComment")]
        [SwaggerResponse(200, "Comment unliked")]
        [SwaggerResponse(401, "Unauthorized")]
        [SwaggerResponse(404, "Comment not found, deleted, or has 0 likes")]
        public async Task<IActionResult> UnlikeComment(Guid id)
        {
            var success = await _commentService.UnlikeCommentAsync(id);
            if (!success) return NotFound(new BaseResponse<string>(false, "Comment not found or has 0 likes.", null));
            return Ok(new BaseResponse<string>(true, "Comment unliked.", null));
        }

        // Helper method to extract User ID from JWT Token (Keeping for backward compatibility if needed)
        private Guid GetUserIdFromToken()
        {
            var idString = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);
            if (string.IsNullOrEmpty(idString) || !Guid.TryParse(idString, out Guid userId)) return Guid.Empty;
            return userId;
        }

        // Helper method to extract User Role from JWT Token
        private string GetUserRoleFromToken()
        {
            return User.FindFirstValue(ClaimTypes.Role) ?? "Reader";
        }
    }
}
