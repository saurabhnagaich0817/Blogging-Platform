using System.Security.Claims;
using InkWell.MediaService.DTOs;
using InkWell.MediaService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using InkWell.Shared.Responses;

namespace InkWell.MediaService.Controllers
{
    [ApiController]
    [Route("api/media")]
    public class MediaController : ControllerBase
    {
        private readonly IMediaService _mediaService;

        public MediaController(IMediaService mediaService)
        {
            _mediaService = mediaService;
        }

        [HttpPost("upload")]
        [Authorize]
        [SwaggerOperation(Summary = "Upload media file", Description = "Uploads an image file (JPG/PNG/WEBP). Max 10MB. Requires Authorization.")]
        [SwaggerResponse(201, "Media uploaded successfully", typeof(MediaResponseDTO))]
        [SwaggerResponse(400, "Invalid file type or size")]
        [SwaggerResponse(401, "Unauthorized")]
        [Consumes("multipart/form-data")] // Crucial for Swagger to render the file upload button
        public async Task<IActionResult> UploadMedia([FromForm] UploadMediaDTO request)
        {
            try
            {
                var uploaderId = GetUserIdFromToken();
                var response = await _mediaService.UploadMediaAsync(request, uploaderId);
                return CreatedAtAction(nameof(GetMedia), new { id = response.MediaId }, new BaseResponse<MediaResponseDTO>(true, "Media uploaded successfully", response));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new BaseResponse<string>(false, ex.Message, null));
            }
        }

        [HttpGet]
        [SwaggerOperation(Summary = "Get all media", Description = "Returns metadata for all uploaded media files.")]
        [SwaggerResponse(200, "List of media", typeof(IEnumerable<MediaResponseDTO>))]
        public async Task<IActionResult> GetAllMedia()
        {
            var media = await _mediaService.GetAllMediaAsync();
            return Ok(new BaseResponse<IEnumerable<MediaResponseDTO>>(true, "Media fetched successfully", media));
        }

        [HttpGet("{id}")]
        [SwaggerOperation(Summary = "Get media by ID", Description = "Fetches metadata details of a specific media file.")]
        [SwaggerResponse(200, "Media found", typeof(MediaResponseDTO))]
        [SwaggerResponse(404, "Media not found")]
        public async Task<IActionResult> GetMedia(Guid id)
        {
            var media = await _mediaService.GetMediaByIdAsync(id);
            if (media == null) return NotFound(new BaseResponse<string>(false, "Media not found", null));
            return Ok(new BaseResponse<MediaResponseDTO>(true, "Media fetched successfully", media));
        }

        [HttpGet("post/{postId}")]
        [SwaggerOperation(Summary = "Get media for a post", Description = "Returns all media items linked to a specific post.")]
        [SwaggerResponse(200, "List of media", typeof(IEnumerable<MediaResponseDTO>))]
        public async Task<IActionResult> GetMediaByPost(Guid postId)
        {
            var media = await _mediaService.GetMediaByPostAsync(postId);
            return Ok(new BaseResponse<IEnumerable<MediaResponseDTO>>(true, "Media fetched successfully", media));
        }

        [HttpGet("user/{userId}")]
        [SwaggerOperation(Summary = "Get media for a user", Description = "Returns all media items uploaded by a specific user.")]
        [SwaggerResponse(200, "List of media", typeof(IEnumerable<MediaResponseDTO>))]
        public async Task<IActionResult> GetMediaByUser(Guid userId)
        {
            var media = await _mediaService.GetMediaByUserAsync(userId);
            return Ok(new BaseResponse<IEnumerable<MediaResponseDTO>>(true, "Media fetched successfully", media));
        }

        [HttpPut("{id}/alt-text")]
        [Authorize]
        [SwaggerOperation(Summary = "Update Alt Text", Description = "Updates the SEO alternate text for an image. Only the owner can update it.")]
        [SwaggerResponse(200, "Alt text updated", typeof(MediaResponseDTO))]
        [SwaggerResponse(401, "Unauthorized")]
        [SwaggerResponse(403, "Forbidden - Only owner can update")]
        public async Task<IActionResult> UpdateAltText(Guid id, [FromBody] UpdateAltTextDTO request)
        {
            var uploaderId = GetUserIdFromToken();
            var response = await _mediaService.UpdateAltTextAsync(id, request.AltText, uploaderId);
            
            if (response == null) return StatusCode(403, new BaseResponse<string>(false, "You do not have permission to update this media.", null));
            return Ok(new BaseResponse<MediaResponseDTO>(true, "Alt text updated successfully", response));
        }

        [HttpDelete("{id}")]
        [Authorize]
        [SwaggerOperation(Summary = "Delete media", Description = "Soft deletes a media file. Only the owner or Admin can delete.")]
        [SwaggerResponse(204, "Deleted")]
        [SwaggerResponse(401, "Unauthorized")]
        [SwaggerResponse(403, "Forbidden - Insufficient permissions")]
        public async Task<IActionResult> DeleteMedia(Guid id)
        {
            var uploaderId = GetUserIdFromToken();
            var role = GetUserRoleFromToken();

            var success = await _mediaService.DeleteMediaAsync(id, uploaderId, role);
            if (!success) return StatusCode(403, new BaseResponse<string>(false, "You do not have permission to delete this media.", null));

            return Ok(new BaseResponse<string>(true, "Media deleted successfully", null));
        }

        private Guid GetUserIdFromToken()
        {
            var idString = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);
            return Guid.Parse(idString!);
        }

        private string GetUserRoleFromToken()
        {
            return User.FindFirstValue(ClaimTypes.Role) ?? "Reader";
        }
    }
}
