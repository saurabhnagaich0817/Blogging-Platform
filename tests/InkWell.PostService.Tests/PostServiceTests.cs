using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using FluentAssertions;
using InkWell.PostService.Services;
using InkWell.PostService.Repositories;
using InkWell.PostService.DTOs;
using InkWell.PostService.Models;
using MassTransit;
using InkWell.Shared.Events;

namespace InkWell.PostService.Tests
{
    [TestClass]
    public class PostServiceTests
    {
        private Mock<IPostRepository>? _postRepositoryMock;
        private Mock<IPublishEndpoint>? _publishEndpointMock;
        private InkWell.PostService.Services.PostService? _postService;

        [TestInitialize]
        public void Setup()
        {
            _postRepositoryMock = new Mock<IPostRepository>();
            _publishEndpointMock = new Mock<IPublishEndpoint>();

            _postService = new InkWell.PostService.Services.PostService(
                _postRepositoryMock.Object, 
                _publishEndpointMock.Object);
        }

        // [TEST 1]: Create post and verify slug generation + Event publishing.
        [TestMethod]
        public async Task CreatePostAsync_ShouldGenerateCorrectSlug_AndPublishEvent()
        {
            var dto = new CreatePostDTO { Title = "My First Blog Post", Content = "Content", AuthorName = "Saurabh" };
            _postRepositoryMock!.Setup(repo => repo.SlugExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
            _postRepositoryMock.Setup(repo => repo.CreatePostAsync(It.IsAny<Post>())).ReturnsAsync((Post p) => p);

            var result = await _postService!.CreatePostAsync(dto, Guid.NewGuid());

            result.Slug.Should().Be("my-first-blog-post");
            _publishEndpointMock!.Verify(p => p.Publish(It.IsAny<PostCreatedEvent>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        // [TEST 2]: Handle duplicate slugs by adding numeric suffix.
        [TestMethod]
        public async Task CreatePostAsync_ShouldHandleDuplicateSlugs_ByAddingSuffix()
        {
            var dto = new CreatePostDTO { Title = "Duplicate Title", Content = "Content" };
            _postRepositoryMock!.SetupSequence(repo => repo.SlugExistsAsync(It.IsAny<string>())).ReturnsAsync(true).ReturnsAsync(false);
            _postRepositoryMock.Setup(repo => repo.CreatePostAsync(It.IsAny<Post>())).ReturnsAsync((Post p) => p);

            var result = await _postService!.CreatePostAsync(dto, Guid.NewGuid());

            result.Slug.Should().Be("duplicate-title-1");
        }

        // [TEST 3]: Toggle like and verify event publishing.
        [TestMethod]
        public async Task ToggleLikeAsync_ShouldPublishLikedEvent()
        {
            var postId = Guid.NewGuid();
            var post = new Post { PostId = postId, AuthorId = Guid.NewGuid(), LikesCount = 5 };
            _postRepositoryMock!.Setup(repo => repo.ToggleLikeAsync(postId, It.IsAny<Guid>())).ReturnsAsync(post);

            await _postService!.ToggleLikeAsync(postId, Guid.NewGuid(), "Liker");

            _publishEndpointMock!.Verify(p => p.Publish(It.IsAny<PostLikedEvent>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        // [TEST 4]: Get post by slug.
        [TestMethod]
        public async Task GetPostBySlugAsync_ShouldReturnPost_WhenSlugExists()
        {
            var post = new Post { PostId = Guid.NewGuid(), Slug = "test-slug", Title = "Test" };
            _postRepositoryMock!.Setup(repo => repo.GetPostBySlugAsync("test-slug")).ReturnsAsync(post);

            var result = await _postService!.GetPostBySlugAsync("test-slug");

            result.Should().NotBeNull();
            result!.Slug.Should().Be("test-slug");
        }

        // [TEST 5]: Update post content with security check.
        [TestMethod]
        public async Task UpdatePostAsync_ShouldUpdateContent_WhenAuthorIsOwner()
        {
            var postId = Guid.NewGuid();
            var authorId = Guid.NewGuid();
            var existingPost = new Post { PostId = postId, AuthorId = authorId, Title = "Old", Content = "Old", Slug = "old" };
            var updateDto = new UpdatePostDTO { Title = "New", Content = "New" };

            _postRepositoryMock!.Setup(repo => repo.GetPostByIdAsync(postId)).ReturnsAsync(existingPost);
            _postRepositoryMock.Setup(repo => repo.SlugExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
            _postRepositoryMock.Setup(repo => repo.UpdatePostAsync(It.IsAny<Post>())).ReturnsAsync((Post p) => p);

            var result = await _postService!.UpdatePostAsync(postId, updateDto, authorId, "Reader");

            result!.Title.Should().Be("New");
        }

        // [TEST 6]: Admin should be able to delete any post.
        [TestMethod]
        public async Task DeletePostAsync_ShouldReturnTrue_WhenUserIsAdmin()
        {
            var postId = Guid.NewGuid();
            var authorId = Guid.NewGuid(); // Post owner
            var adminId = Guid.NewGuid();
            var post = new Post { PostId = postId, AuthorId = authorId };

            _postRepositoryMock!.Setup(repo => repo.GetPostByIdAsync(postId)).ReturnsAsync(post);
            _postRepositoryMock.Setup(repo => repo.DeletePostAsync(It.IsAny<Post>())).Returns(Task.CompletedTask);

            var result = await _postService!.DeletePostAsync(postId, adminId, "Admin");

            result.Should().BeTrue();
        }

        // [TEST 7]: Return null if post update attempted by someone else.
        [TestMethod]
        public async Task UpdatePostAsync_ShouldReturnNull_WhenUserIsNotOwner()
        {
            var postId = Guid.NewGuid();
            var authorId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();
            var existingPost = new Post { PostId = postId, AuthorId = authorId };

            _postRepositoryMock!.Setup(repo => repo.GetPostByIdAsync(postId)).ReturnsAsync(existingPost);

            var result = await _postService!.UpdatePostAsync(postId, new UpdatePostDTO(), otherUserId, "Reader");

            result.Should().BeNull();
        }

        // [TEST 8]: Get all posts.
        [TestMethod]
        public async Task GetAllPostsAsync_ShouldReturnList()
        {
            var posts = new List<Post> { new Post { Title = "P1" }, new Post { Title = "P2" } };
            _postRepositoryMock!.Setup(repo => repo.GetAllPostsAsync()).ReturnsAsync(posts);

            var result = await _postService!.GetAllPostsAsync();

            result.Count().Should().Be(2);
        }

        // [TEST 9]: Toggle Save functionality.
        [TestMethod]
        public async Task ToggleSaveAsync_ShouldCallRepository()
        {
            var postId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            _postRepositoryMock!.Setup(repo => repo.ToggleSaveAsync(postId, userId)).ReturnsAsync(true);

            var result = await _postService!.ToggleSaveAsync(postId, userId);

            result.Should().BeTrue();
            _postRepositoryMock.Verify(repo => repo.ToggleSaveAsync(postId, userId), Times.Once);
        }

        // [TEST 10]: Get post by ID.
        [TestMethod]
        public async Task GetPostByIdAsync_ShouldReturnPost()
        {
            var postId = Guid.NewGuid();
            var post = new Post { PostId = postId, Title = "Title" };
            _postRepositoryMock!.Setup(repo => repo.GetPostByIdAsync(postId)).ReturnsAsync(post);

            var result = await _postService!.GetPostByIdAsync(postId);

            result.Should().NotBeNull();
            result!.Title.Should().Be("Title");
        }
    }
}
