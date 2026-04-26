using InkWell.CategoryService.DTOs;
using InkWell.CategoryService.Services;
using InkWell.Shared.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace InkWell.CategoryService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class TagsController : ControllerBase
    {
        private readonly ICategoryService _service;

        public TagsController(ICategoryService service)
        {
            _service = service;
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Author")]
        [SwaggerOperation(Summary = "Create a Tag", Description = "Creates a new tag.")]
        [SwaggerResponse(201, "Tag created successfully", typeof(TagResponseDTO))]
        public async Task<IActionResult> CreateTag([FromBody] CreateTagDTO dto)
        {
            var response = await _service.CreateTagAsync(dto);
            return CreatedAtAction(nameof(GetAllTags), new BaseResponse<TagResponseDTO>(true, "Tag created successfully", response));
        }

        [HttpGet]
        [SwaggerOperation(Summary = "Get all tags", Description = "Fetches all tags.")]
        [SwaggerResponse(200, "List of tags", typeof(IEnumerable<TagResponseDTO>))]
        public async Task<IActionResult> GetAllTags()
        {
            var tags = await _service.GetAllTagsAsync();
            return Ok(new BaseResponse<IEnumerable<TagResponseDTO>>(true, "Tags fetched successfully", tags));
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin")]
        [SwaggerOperation(Summary = "Delete a tag", Description = "Deletes a tag. Admin only.")]
        public async Task<IActionResult> DeleteTag(Guid id)
        {
            var success = await _service.DeleteTagAsync(id);
            if (!success)
            {
                return NotFound(new BaseResponse<string>(false, "Tag not found", null));
            }

            return Ok(new BaseResponse<string>(true, "Tag deleted successfully", null));
        }

        [HttpPost("add-to-post")]
        [Authorize(Roles = "Admin,Author")]
        [SwaggerOperation(Summary = "Assign tag to post", Description = "Maps a tag to a post and increments the tag's PostCount.")]
        public async Task<IActionResult> AddTagToPost([FromQuery] Guid postId, [FromQuery] Guid tagId)
        {
            var success = await _service.AddTagToPostAsync(postId, tagId);
            if (!success)
            {
                return BadRequest(new BaseResponse<string>(false, "Tag or Post not found.", null));
            }

            return Ok(new BaseResponse<string>(true, "Tag added to post successfully.", null));
        }

        [HttpDelete("remove-from-post")]
        [Authorize(Roles = "Admin,Author")]
        [SwaggerOperation(Summary = "Remove tag from post", Description = "Unmaps a tag from a post and decrements the tag's PostCount.")]
        public async Task<IActionResult> RemoveTagFromPost([FromQuery] Guid postId, [FromQuery] Guid tagId)
        {
            var success = await _service.RemoveTagFromPostAsync(postId, tagId);
            if (!success)
            {
                return BadRequest(new BaseResponse<string>(false, "Mapping not found.", null));
            }

            return Ok(new BaseResponse<string>(true, "Tag removed successfully.", null));
        }

        [HttpGet("post/{postId:guid}")]
        [SwaggerOperation(Summary = "Get tags for a post", Description = "Returns all tags associated with a specific post.")]
        [SwaggerResponse(200, "List of tags for post", typeof(IEnumerable<TagResponseDTO>))]
        public async Task<IActionResult> GetTagsByPost(Guid postId)
        {
            var tags = await _service.GetTagsByPostAsync(postId);
            return Ok(new BaseResponse<IEnumerable<TagResponseDTO>>(true, "Tags fetched successfully", tags));
        }

        [HttpGet("trending")]
        [SwaggerOperation(Summary = "Get trending tags", Description = "Returns tags sorted by highest PostCount.")]
        [SwaggerResponse(200, "Trending tags", typeof(IEnumerable<TagResponseDTO>))]
        public async Task<IActionResult> GetTrendingTags([FromQuery] int count = 5)
        {
            var tags = await _service.GetTrendingTagsAsync(count);
            return Ok(new BaseResponse<IEnumerable<TagResponseDTO>>(true, "Trending tags fetched successfully", tags));
        }
    }
}
