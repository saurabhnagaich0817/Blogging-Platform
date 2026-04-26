using InkWell.PostService.DTOs;
using InkWell.PostService.Models;
using InkWell.PostService.Repositories;
using MassTransit;
using InkWell.Shared.Events;

namespace InkWell.PostService.Services
{
    /// <summary>
    /// Service responsible for handling story/post management, including CRUD operations, 
    /// engagement tracking (likes), and analytics.
    /// </summary>
    public class PostService : IPostService
    {
        private const string DefaultPostStatus = "Published";
        private readonly IPostRepository _repository;
        private readonly IPublishEndpoint _publishEndpoint;

        public PostService(IPostRepository repository, IPublishEndpoint publishEndpoint)
        {
            _repository = repository;
            _publishEndpoint = publishEndpoint;
        }

        /// <summary>
        /// Creates a new post and publishes a PostCreatedEvent for downstream services.
        /// </summary>
        public async Task<PostResponseDTO> CreatePostAsync(CreatePostDTO dto, Guid authorId)
        {
            var title = dto.Title.Trim();
            var content = dto.Content.Trim();
            var slug = await GenerateUniqueSlug(title);

            var post = new Post
            {
                PostId = Guid.NewGuid(),
                AuthorId = authorId,
                AuthorName = dto.AuthorName ?? "Anonymous",
                AuthorEmail = dto.AuthorEmail ?? string.Empty,
                Title = title,
                Content = content,
                ImageUrl = dto.ImageUrl?.Trim() ?? string.Empty,
                CategoryId = dto.CategoryId,
                CategoryName = dto.CategoryName,
                Status = NormalizeStatus(dto.Status),
                Slug = slug,
                CreatedAt = DateTime.UtcNow
            };

            var createdPost = await _repository.CreatePostAsync(post);

            // Publish PostCreatedEvent
            await _publishEndpoint.Publish(new PostCreatedEvent
            {
                PostId = createdPost.PostId,
                AuthorId = createdPost.AuthorId,
                Title = createdPost.Title ?? string.Empty,
                Slug = createdPost.Slug ?? string.Empty
            });

            return MapToResponseDTO(createdPost);
        }

        public async Task<PostResponseDTO?> GetPostByIdAsync(Guid id)
        {
            var post = await _repository.GetPostByIdAsync(id);
            return post == null ? null : MapToResponseDTO(post);
        }

        public async Task<IEnumerable<PostResponseDTO>> GetAllPostsAsync()
        {
            var posts = await _repository.GetAllPostsAsync();
            return posts
                .OrderByDescending(post => post.CreatedAt)
                .Select(MapToResponseDTO);
        }

        public async Task<PostResponseDTO?> UpdatePostAsync(Guid id, UpdatePostDTO dto, Guid authorId, string role)
        {
            var post = await _repository.GetPostByIdAsync(id);
            
            // Post doesn't exist or doesn't belong to this author (and not Admin)
            if (post == null || (post.AuthorId != authorId && role != "Admin")) return null;

            if (!string.IsNullOrWhiteSpace(dto.Title))
            {
                post.Title = dto.Title.Trim();
                post.Slug = await GenerateUniqueSlug(post.Title, post.PostId);
            }

            if (!string.IsNullOrWhiteSpace(dto.Content))
            {
                post.Content = dto.Content.Trim();
            }

            if (!string.IsNullOrWhiteSpace(dto.Status))
            {
                post.Status = NormalizeStatus(dto.Status);
            }

            if (dto.ImageUrl != null)
            {
                post.ImageUrl = dto.ImageUrl;
            }

            if (dto.CategoryId.HasValue)
            {
                post.CategoryId = dto.CategoryId.Value;
                post.CategoryName = dto.CategoryName;
            }

            post.UpdatedAt = DateTime.UtcNow;

            var updatedPost = await _repository.UpdatePostAsync(post);
            return MapToResponseDTO(updatedPost);
        }

        public async Task<bool> DeletePostAsync(Guid id, Guid authorId, string role)
        {
            var post = await _repository.GetPostByIdAsync(id);
            if (post == null || (post.AuthorId != authorId && role != "Admin")) return false;

            await _repository.DeletePostAsync(post);
            return true;
        }

        /// <summary>
        /// Toggles a like on a post and broadcasts a PostLikedEvent for notifications.
        /// </summary>
        public async Task<int> ToggleLikeAsync(Guid postId, Guid userId, string likerName)
        {
            var post = await _repository.ToggleLikeAsync(postId, userId);
            if (post != null)
            {
                await _publishEndpoint.Publish(new PostLikedEvent
                {
                    PostId = post.PostId,
                    UserId = userId,
                    LikerName = likerName,
                    PostAuthorId = post.AuthorId,
                    PostAuthorEmail = post.AuthorEmail ?? string.Empty
                });
                return post.LikesCount;
            }
            return 0;
        }

        public async Task<bool> ToggleSaveAsync(Guid postId, Guid userId)
        {
            return await _repository.ToggleSaveAsync(postId, userId);
        }

        public async Task<IEnumerable<PostResponseDTO>> GetSavedPostsAsync(Guid userId)
        {
            var posts = await _repository.GetSavedPostsAsync(userId);
            return posts
                .OrderByDescending(post => post.CreatedAt)
                .Select(MapToResponseDTO);
        }

        public async Task<IEnumerable<PostResponseDTO>> GetMyPostsAsync(Guid authorId)
        {
            var posts = await _repository.GetPostsByAuthorAsync(authorId);
            return posts
                .OrderByDescending(post => post.CreatedAt)
                .Select(MapToResponseDTO);
        }

        public async Task<PostResponseDTO?> GetPostBySlugAsync(string slug)
        {
            var post = await _repository.GetPostBySlugAsync(slug);
            return post == null ? null : MapToResponseDTO(post);
        }

        public async Task<AnalyticsResponseDTO> GetAnalyticsAsync(Guid? authorId = null)
        {
            var posts = await _repository.GetAllPostsAsync();
            
            // Filter by author if requested
            if (authorId.HasValue)
            {
                posts = posts.Where(p => p.AuthorId == authorId.Value).ToList();
            }

            var totalStories = posts.Count();
            var totalEngagement = posts.Sum(p => p.LikesCount);
            
            // Mock trending stories (top 5 by likes)
            var trendingStories = posts.OrderByDescending(p => p.LikesCount)
                .Take(5)
                .Select(p => new TrendingStoryDTO
                {
                    PostId = p.PostId,
                    Title = p.Title,
                    Likes = p.LikesCount,
                    Views = p.LikesCount * 10 // Mock views
                });

            return new AnalyticsResponseDTO
            {
                TotalStories = totalStories,
                TotalEngagement = totalEngagement,
                StorageUsagePercentage = 12.5, 
                TrendingStories = trendingStories,
                SixthOccurrencePosition = totalStories > 0 ? (totalStories * 3) % 100 : 0 
            };
        }

        // Logic to generate slug: "Hello World" -> "hello-world"
        private async Task<string> GenerateUniqueSlug(string title, Guid? currentPostId = null)
        {
            var slugBase = new string(title
                .Trim()
                .ToLowerInvariant()
                .Replace(" ", "-")
                .Where(c => char.IsLetterOrDigit(c) || c == '-')
                .ToArray());

            if (string.IsNullOrWhiteSpace(slugBase))
            {
                slugBase = "post";
            }

            int count = 1;
            var slug = slugBase;

            while (await _repository.SlugExistsAsync(slug))
            {
                var existingPost = await _repository.GetPostBySlugAsync(slug);
                if (existingPost?.PostId == currentPostId)
                {
                    break;
                }

                slug = $"{slugBase}-{count}";
                count++;
            }

            return slug;
        }

        private static string NormalizeStatus(string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                return DefaultPostStatus;
            }

            return status.Trim() switch
            {
                "Draft" => "Draft",
                "Published" => "Published",
                "Archived" => "Archived",
                _ => DefaultPostStatus
            };
        }

        private PostResponseDTO MapToResponseDTO(Post post)
        {
            return new PostResponseDTO
            {
                PostId = post.PostId,
                AuthorId = post.AuthorId,
                AuthorName = post.AuthorName,
                Title = post.Title,
                Slug = post.Slug,
                Content = post.Content,
                ImageUrl = post.ImageUrl,
                LikesCount = post.LikesCount,
                Status = post.Status,
                CategoryId = post.CategoryId,
                CategoryName = post.CategoryName,
                CreatedAt = post.CreatedAt,
                UpdatedAt = post.UpdatedAt
            };
        }
    }
}
