using InkWell.CategoryService.DTOs;
using InkWell.CategoryService.Services;
using InkWell.Shared.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace InkWell.CategoryService.Controllers
{
    /// <summary>
    /// Controller for managing post tags, trending topics, and post associations.
    /// </summary>
    [ApiController]
    [Route("api/tags")]
    [Produces("application/json")]
    public class TagsController : ControllerBase
    {
        private readonly ICategoryService _service;

        public TagsController(ICategoryService service)
        {
            _service = service;
        }

        /// <summary>
        /// Creates a new tag in the platform metadata.
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin,Author")]
        [SwaggerOperation(Summary = "Create a new tag", Description = "Adds a new tag for use in posts. Restricted to Admins and Authors.")]
        public async Task<IActionResult> CreateTag([FromBody] CreateTagDTO dto)
        {
            var response = await _service.CreateTagAsync(dto);
            return CreatedAtAction(nameof(GetAllTags), new BaseResponse<TagResponseDTO>(true, "Tag created successfully", response));
        }

        /// <summary>
        /// Retrieves all available tags.
        /// </summary>
        [HttpGet]
        [SwaggerOperation(Summary = "Get all tags", Description = "Fetches a complete list of tags available for categorization.")]
        public async Task<IActionResult> GetAllTags()
        {
            var tags = await _service.GetAllTagsAsync();
            return Ok(new BaseResponse<IEnumerable<TagResponseDTO>>(true, "Tags fetched successfully", tags));
        }

        /// <summary>
        /// Deletes a specific tag. Restricted to Administrators.
        /// </summary>
        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin")]
        [SwaggerOperation(Summary = "Delete a tag", Description = "Admin only. Permanently removes a tag from the system.")]
        public async Task<IActionResult> DeleteTag(Guid id)
        {
            var success = await _service.DeleteTagAsync(id);
            if (!success) return NotFound(new BaseResponse<string>(false, "Tag not found", null));
            return Ok(new BaseResponse<string>(true, "Tag deleted successfully", null));
        }

        /// <summary>
        /// Associates a tag with a specific post.
        /// </summary>
        [HttpPost("add-to-post")]
        [Authorize(Roles = "Admin,Author")]
        [SwaggerOperation(Summary = "Assign tag to post", Description = "Maps a tag to a post and updates engagement counts.")]
        public async Task<IActionResult> AddTagToPost([FromQuery] Guid postId, [FromQuery] Guid tagId)
        {
            var success = await _service.AddTagToPostAsync(postId, tagId);
            if (!success) return BadRequest(new BaseResponse<string>(false, "Target tag or post not found.", null));
            return Ok(new BaseResponse<string>(true, "Tag associated with post successfully.", null));
        }

        /// <summary>
        /// Removes a tag association from a post.
        /// </summary>
        [HttpDelete("remove-from-post")]
        [Authorize(Roles = "Admin,Author")]
        [SwaggerOperation(Summary = "Remove tag from post", Description = "Unmaps a tag from a specific post.")]
        public async Task<IActionResult> RemoveTagFromPost([FromQuery] Guid postId, [FromQuery] Guid tagId)
        {
            var success = await _service.RemoveTagFromPostAsync(postId, tagId);
            if (!success) return BadRequest(new BaseResponse<string>(false, "Tag mapping not found.", null));
            return Ok(new BaseResponse<string>(true, "Tag removed from post successfully.", null));
        }

        /// <summary>
        /// Retrieves all tags associated with a specific post.
        /// </summary>
        [HttpGet("post/{postId:guid}")]
        [SwaggerOperation(Summary = "Get tags for a post", Description = "Returns all tags mapped to the specified post.")]
        public async Task<IActionResult> GetTagsByPost(Guid postId)
        {
            var tags = await _service.GetTagsByPostAsync(postId);
            return Ok(new BaseResponse<IEnumerable<TagResponseDTO>>(true, "Post tags fetched successfully", tags));
        }

        /// <summary>
        /// Retrieves the most popular tags based on post count.
        /// </summary>
        [HttpGet("trending")]
        [SwaggerOperation(Summary = "Get trending tags", Description = "Returns top tags sorted by usage across the platform.")]
        public async Task<IActionResult> GetTrendingTags([FromQuery] int count = 5)
        {
            var tags = await _service.GetTrendingTagsAsync(count);
            return Ok(new BaseResponse<IEnumerable<TagResponseDTO>>(true, "Trending tags fetched successfully", tags));
        }
    }
}
