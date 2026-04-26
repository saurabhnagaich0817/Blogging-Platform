using System.Security.Claims;
using InkWell.PostService.DTOs;
using InkWell.PostService.Services;
using InkWell.Shared.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace InkWell.PostService.Controllers
{
    [ApiController]
    [Route("api/posts")]
    public class PostController : ControllerBase
    {
        private readonly IPostService _postService;

        public PostController(IPostService postService)
        {
            _postService = postService;
        }

        /// <summary>
        /// Retrieves dashboard analytics, including total engagement and trending stories.
        /// </summary>
        [HttpGet("analytics")]
        [SwaggerOperation(Summary = "Get analytics data", Description = "Returns dashboard stats, trending stories, etc.")]
        public async Task<IActionResult> GetAnalytics()
        {
            Guid? authorId = null;
            if (TryGetCurrentUserId(out var userId))
            {
                var role = User.FindFirstValue(ClaimTypes.Role) ?? User.FindFirstValue("role");
                if (role == "Author")
                {
                    authorId = userId;
                }
            }

            var analytics = await _postService.GetAnalyticsAsync(authorId);
            return Ok(new BaseResponse<AnalyticsResponseDTO>(true, "Analytics fetched successfully", analytics));
        }

        /// <summary>
        /// Publishes a new story to the platform. Requires Author or Admin roles.
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Author,Admin")]
        [SwaggerOperation(Summary = "Create a new blog post", Description = "Creates a new post for the currently authenticated user.")]
        public async Task<IActionResult> CreatePost([FromBody] CreatePostDTO request)
        {
            // First, we try to get the User ID from the logged-in user's JWT token.
            if (!TryGetCurrentUserId(out var authorId))
            {
                // If we can't find the user ID, it means the user is not authenticated properly.
                return Unauthorized(new BaseResponse<string>(false, "Invalid or missing user ID in token.", null));
            }

            // Call the service to save the post in the database.
            var response = await _postService.CreatePostAsync(request, authorId);
            
            // Return a '201 Created' response with the newly created post data.
            return CreatedAtAction(nameof(GetPost), new { id = response.PostId }, new BaseResponse<PostResponseDTO>(true, "Post created successfully", response));
        }

        [HttpGet]
        [SwaggerOperation(Summary = "Fetch all posts", Description = "Retrieves a list of all blog posts.")]
        public async Task<IActionResult> GetAllPosts()
        {
            var posts = await _postService.GetAllPostsAsync();
            return Ok(new BaseResponse<IEnumerable<PostResponseDTO>>(true, "Posts fetched successfully", posts));
        }

        [HttpPost("seed-data")]
        [SwaggerOperation(Summary = "Seed initial posts", Description = "Populates the database with 20 professional blog posts for demonstration.")]
        public async Task<IActionResult> SeedData()
        {
            var existing = await _postService.GetAllPostsAsync();
            if (existing.Any()) return BadRequest(new BaseResponse<string>(false, "Database already has posts.", null));

            var authorId = Guid.NewGuid();
            string[] titles = {
                "The Future of Microservices in 2026", "Why Angular 17 is a Game Changer", "Mastering .NET 8 Web APIs",
                "The Rise of Agentic AI Coding Assistants", "Design Systems: From Sketch to Code", "Clean Architecture in Modern Applications",
                "RabbitMQ vs Kafka: Which to Choose?", "The Art of Writing Clean JavaScript", "Tailwind CSS: Beyond the Basics",
                "Building Scalable Systems with YARP", "The Impact of AI on Software Engineering", "Why I Switched to Glassmorphism UI",
                "The Secret to Productive Remote Work", "Understanding JWT Authentication", "Responsive Design in the Era of Foldables",
                "Docker Tips for Faster Development", "The Evolution of Cloud Computing", "Building InkWell: A Dev Log",
                "Writing for the Web: A Guide", "SEO Best Practices for Bloggers"
            };

            string[] contents = {
                "Microservices continue to evolve. In 2026, we see more focus on service meshes and automated observability...",
                "Angular 17 introduced signals and a new control flow that makes it faster and more intuitive than ever...",
                ".NET 8 brings performance improvements that are staggering. From Native AOT to improved JSON serialization...",
                "AI is no longer just a chatbot; it's an agent that can write, test, and deploy code alongside you...",
                "A good design system is more than just colors and fonts. It's about consistency and scalability...",
                "Separating concerns is the heart of Clean Architecture. It makes your code testable and maintainable...",
                "RabbitMQ is great for complex routing, while Kafka shines in high-throughput event streaming...",
                "Clean code is not about following rules; it's about making your code readable for the next human...",
                "Tailwind is more than just utility classes. It's a system for building consistent interfaces quickly...",
                "YARP (Yet Another Reverse Proxy) provides a powerful toolkit for building high-performance gateways...",
                "Artificial Intelligence is changing the landscape of software engineering by automating routine tasks...",
                "Glassmorphism creates a sense of depth and hierarchy that feels premium and modern...",
                "Remote work is here to stay. The key is finding a balance between deep work and collaboration...",
                "JSON Web Tokens are the standard for secure authentication in stateless microservices...",
                "With new device forms, responsive design must adapt to multiple screens and folding hinges...",
                "Small changes in your Dockerfile can lead to massive improvements in build times...",
                "Cloud computing has moved from just hosting to providing specialized AI and serverless services...",
                "Building a platform like InkWell requires careful planning of microservices and event flows...",
                "Web content should be scannable, concise, and focused on the user's needs...",
                "SEO is not just about keywords; it's about providing value and having a fast, accessible site..."
            };

            string[] images = {
                "https://images.unsplash.com/photo-1451187580459-43490279c0fa?q=80&w=1000", "https://images.unsplash.com/photo-1555066931-4365d14bab8c?q=80&w=1000",
                "https://images.unsplash.com/photo-1517694712202-14dd9538aa97?q=80&w=1000", "https://images.unsplash.com/photo-1677442136019-21780ecad995?q=80&w=1000",
                "https://images.unsplash.com/photo-1586717791821-3f44a563dc4c?q=80&w=1000", "https://images.unsplash.com/photo-1618477388954-7852f32655ec?q=80&w=1000",
                "https://images.unsplash.com/photo-1558494949-ef010cbdcc51?q=80&w=1000", "https://images.unsplash.com/photo-1516116216624-53e697fedbea?q=80&w=1000",
                "https://images.unsplash.com/photo-1587620962725-abab7fe55159?q=80&w=1000", "https://images.unsplash.com/photo-1550751827-4bd374c3f58b?q=80&w=1000",
                "https://images.unsplash.com/photo-1485827404703-89b55fcc595e?q=80&w=1000", "https://images.unsplash.com/photo-1558591710-4b4a1ae0f04d?q=80&w=1000",
                "https://images.unsplash.com/photo-1522202176988-66273c2fd55f?q=80&w=1000", "https://images.unsplash.com/photo-1563986768609-322da13575f3?q=80&w=1000",
                "https://images.unsplash.com/photo-1483058712412-4245e9b90334?q=80&w=1000", "https://images.unsplash.com/photo-1605745341112-85968b193ef5?q=80&w=1000",
                "https://images.unsplash.com/photo-1544197150-b99a580bb7a8?q=80&w=1000", "https://images.unsplash.com/photo-1499750310107-5fef28a66643?q=80&w=1000",
                "https://images.unsplash.com/photo-1455390582262-044cdead277a?q=80&w=1000", "https://images.unsplash.com/photo-1432888498266-38ffec3eaf0a?q=80&w=1000"
            };

            for (int i = 0; i < titles.Length; i++)
            {
                await _postService.CreatePostAsync(new CreatePostDTO {
                    Title = titles[i],
                    Content = contents[i],
                    ImageUrl = images[i],
                    AuthorName = "InkWell Editorial",
                    Status = "Published"
                }, authorId);
            }

            return Ok(new BaseResponse<string>(true, "20 posts seeded successfully", null));
        }

        [HttpGet("{id}")]
        [SwaggerOperation(Summary = "Get a post by ID", Description = "Retrieves a specific post by its ID.")]
        public async Task<IActionResult> GetPost(Guid id)
        {
            var post = await _postService.GetPostByIdAsync(id);
            if (post == null) return NotFound(new BaseResponse<string>(false, "Post not found.", null));
            return Ok(new BaseResponse<PostResponseDTO>(true, "Post fetched successfully", post));
        }

        [HttpGet("slug/{slug}")]
        [SwaggerOperation(Summary = "Get a post by slug", Description = "Retrieves a specific post by its URL slug.")]
        public async Task<IActionResult> GetPostBySlug(string slug)
        {
            var post = await _postService.GetPostBySlugAsync(slug);
            if (post == null) return NotFound(new BaseResponse<string>(false, "Post not found.", null));
            return Ok(new BaseResponse<PostResponseDTO>(true, "Post fetched successfully", post));
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Author,Admin")]
        [SwaggerOperation(Summary = "Update a post", Description = "Updates an existing post. Only the author can update.")]
        public async Task<IActionResult> UpdatePost(Guid id, [FromBody] UpdatePostDTO request)
        {
            if (!TryGetCurrentUserId(out var authorId))
            {
                return Unauthorized(new BaseResponse<string>(false, "Invalid or missing user ID in token.", null));
            }

            var existingPost = await _postService.GetPostByIdAsync(id);
            if (existingPost == null)
            {
                return NotFound(new BaseResponse<string>(false, "Post not found.", null));
            }

            var role = GetCurrentUserRole();

            var response = await _postService.UpdatePostAsync(id, request, authorId, role);
            if (response == null)
            {
                return StatusCode(403, new BaseResponse<string>(false, "You do not have permission to edit this post.", null));
            }
            
            return Ok(new BaseResponse<PostResponseDTO>(true, "Post updated successfully", response));
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Author,Admin")]
        [SwaggerOperation(Summary = "Delete a post", Description = "Deletes a post. Only the author or an admin can delete.")]
        public async Task<IActionResult> DeletePost(Guid id)
        {
            if (!TryGetCurrentUserId(out var authorId))
            {
                return Unauthorized(new BaseResponse<string>(false, "Invalid or missing user ID in token.", null));
            }

            var existingPost = await _postService.GetPostByIdAsync(id);
            if (existingPost == null)
            {
                return NotFound(new BaseResponse<string>(false, "Post not found.", null));
            }

            var role = GetCurrentUserRole();

            var success = await _postService.DeletePostAsync(id, authorId, role);
            if (!success)
            {
                return StatusCode(403, new BaseResponse<string>(false, "You do not have permission to delete this post.", null));
            }

            return Ok(new BaseResponse<string>(true, "Post deleted successfully", null));
        }

        [HttpPost("{id}/like")]
        [Authorize]
        [SwaggerOperation(Summary = "Like/Unlike a post", Description = "Toggles the like status of a post for the current user.")]
        public async Task<IActionResult> ToggleLike(Guid id)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return Unauthorized(new BaseResponse<string>(false, "Invalid or missing user ID in token.", null));
            }
            
            var likerName = User.FindFirstValue("username") ?? User.FindFirstValue(ClaimTypes.Name) ?? "Someone";
            var likesCount = await _postService.ToggleLikeAsync(id, userId, likerName);
            return Ok(new BaseResponse<int>(true, "Like status toggled", likesCount));
        }

        [HttpPost("{id}/save")]
        [Authorize]
        [SwaggerOperation(Summary = "Save/Unsave a post", Description = "Toggles the saved status of a post for the current user.")]
        public async Task<IActionResult> ToggleSave(Guid id)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return Unauthorized(new BaseResponse<string>(false, "Invalid or missing user ID in token.", null));
            }

            var isSaved = await _postService.ToggleSaveAsync(id, userId);
            return Ok(new BaseResponse<bool>(true, "Save status toggled", isSaved));
        }

        [HttpGet("my")]
        [Authorize]
        [SwaggerOperation(Summary = "Get my posts", Description = "Retrieves all posts created by the current user.")]
        public async Task<IActionResult> GetMyPosts()
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return Unauthorized(new BaseResponse<string>(false, "Invalid or missing user ID in token.", null));
            }

            var posts = await _postService.GetMyPostsAsync(userId);
            return Ok(new BaseResponse<IEnumerable<PostResponseDTO>>(true, "My posts fetched", posts));
        }

        [HttpGet("saved")]
        [Authorize]
        [SwaggerOperation(Summary = "Get saved posts", Description = "Retrieves all posts saved by the current user.")]
        public async Task<IActionResult> GetSavedPosts()
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return Unauthorized(new BaseResponse<string>(false, "Invalid or missing user ID in token.", null));
            }

            var posts = await _postService.GetSavedPostsAsync(userId);
            return Ok(new BaseResponse<IEnumerable<PostResponseDTO>>(true, "Saved posts fetched", posts));
        }

        [HttpPost("{id}/share")]
        [SwaggerOperation(Summary = "Share post", Description = "Records a share action (for analytics).")]
        public async Task<IActionResult> SharePost(Guid id)
        {
            // Placeholder for share analytics logic
            return Ok(new BaseResponse<string>(true, "Share recorded", null));
        }



        private bool TryGetCurrentUserId(out Guid userId)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("userId")
                ?? User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);

            return Guid.TryParse(userIdClaim, out userId);
        }

        private string GetCurrentUserRole()
        {
            return User.FindFirstValue(ClaimTypes.Role)
                ?? User.FindFirstValue("role")
                ?? "Reader";
        }
    }
}
