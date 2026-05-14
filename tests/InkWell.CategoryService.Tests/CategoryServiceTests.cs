using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using FluentAssertions;
using InkWell.CategoryService.Services;
using InkWell.CategoryService.Repositories;
using InkWell.CategoryService.DTOs;
using InkWell.CategoryService.Models;

namespace InkWell.CategoryService.Tests
{
    [TestClass]
    public class CategoryServiceTests
    {
        private Mock<ICategoryRepository>? _categoryRepositoryMock;
        private InkWell.CategoryService.Services.CategoryService? _categoryService;

        [TestInitialize]
        public void Setup()
        {
            _categoryRepositoryMock = new Mock<ICategoryRepository>();
            _categoryService = new InkWell.CategoryService.Services.CategoryService(_categoryRepositoryMock.Object);
        }

        // [TEST 1]: Create category and verify slug is generated correctly.
        [TestMethod]
        public async Task CreateCategoryAsync_ShouldGenerateSlug_AndSaveCategory()
        {
            // Logic: Names like "Web Development" should become "web-development".
            var dto = new CreateCategoryDTO { Name = "Web Development", Description = "Web stuff" };
            _categoryRepositoryMock!.Setup(repo => repo.CategorySlugExistsAsync(It.IsAny<string>())).ReturnsAsync(false);

            var result = await _categoryService!.CreateCategoryAsync(dto);

            result.Slug.Should().Be("web-development");
            _categoryRepositoryMock.Verify(repo => repo.CreateCategoryAsync(It.IsAny<Category>()), Times.Once);
        }

        // [TEST 2]: Handle duplicate slugs by adding a numeric suffix.
        [TestMethod]
        public async Task CreateCategoryAsync_ShouldHandleDuplicateSlugs()
        {
            // Logic: If "news" exists, it should try "news-1".
            var dto = new CreateCategoryDTO { Name = "News" };
            _categoryRepositoryMock!.SetupSequence(repo => repo.CategorySlugExistsAsync(It.IsAny<string>()))
                .ReturnsAsync(true).ReturnsAsync(false);

            var result = await _categoryService!.CreateCategoryAsync(dto);

            result.Slug.Should().Be("news-1");
        }

        // [TEST 3]: Increment tag's post count when added to a post.
        [TestMethod]
        public async Task AddTagToPostAsync_ShouldIncrementPostCount()
        {
            // Logic: Keeps track of how many posts use a certain tag.
            var postId = Guid.NewGuid();
            var tagId = Guid.NewGuid();
            var tag = new Tag { TagId = tagId, Name = "DotNet", PostCount = 5 };

            _categoryRepositoryMock!.Setup(repo => repo.GetTagByIdAsync(tagId)).ReturnsAsync(tag);
            _categoryRepositoryMock.Setup(repo => repo.GetPostTagAsync(postId, tagId)).ReturnsAsync((PostTag?)null);

            await _categoryService!.AddTagToPostAsync(postId, tagId);

            tag.PostCount.Should().Be(6);
            _categoryRepositoryMock.Verify(repo => repo.UpdateTagAsync(tag), Times.Once);
        }

        // [TEST 4]: Don't increment count if tag is already assigned to the post.
        [TestMethod]
        public async Task AddTagToPostAsync_ShouldNotIncrement_IfAlreadyAssigned()
        {
            // Logic: Idempotency check. Adding the same tag twice shouldn't double count.
            var postId = Guid.NewGuid();
            var tagId = Guid.NewGuid();
            var tag = new Tag { TagId = tagId, PostCount = 5 };

            _categoryRepositoryMock!.Setup(repo => repo.GetTagByIdAsync(tagId)).ReturnsAsync(tag);
            _categoryRepositoryMock.Setup(repo => repo.GetPostTagAsync(postId, tagId)).ReturnsAsync(new PostTag());

            await _categoryService!.AddTagToPostAsync(postId, tagId);

            tag.PostCount.Should().Be(5);
        }

        // [TEST 5]: Decrement count when tag is removed from post.
        [TestMethod]
        public async Task RemoveTagFromPostAsync_ShouldDecrementPostCount()
        {
            // Logic: Cleanup check. Count should decrease when post is untagged.
            var postId = Guid.NewGuid();
            var tagId = Guid.NewGuid();
            var tag = new Tag { TagId = tagId, PostCount = 10 };

            _categoryRepositoryMock!.Setup(repo => repo.GetPostTagAsync(postId, tagId)).ReturnsAsync(new PostTag());
            _categoryRepositoryMock.Setup(repo => repo.GetTagByIdAsync(tagId)).ReturnsAsync(tag);

            await _categoryService!.RemoveTagFromPostAsync(postId, tagId);

            tag.PostCount.Should().Be(9);
        }

        // [TEST 6]: Fetch trending tags based on post count.
        [TestMethod]
        public async Task GetTrendingTagsAsync_ShouldReturnTopTags()
        {
            // Logic: Analytics check. Verify it calls the repository to get popular tags.
            var tags = new List<Tag> { new Tag { Name = "Hot", PostCount = 100 } };
            _categoryRepositoryMock!.Setup(repo => repo.GetTrendingTagsAsync(5)).ReturnsAsync(tags);

            var result = await _categoryService!.GetTrendingTagsAsync(5);

            result.Count().Should().Be(1);
        }

        // [TEST 7]: Delete category by ID.
        [TestMethod]
        public async Task DeleteCategoryAsync_ShouldReturnTrue_WhenCategoryExists()
        {
            // Logic: Lifecycle check. Category should be removable.
            var catId = Guid.NewGuid();
            var cats = new List<Category> { new Category { CategoryId = catId } };
            _categoryRepositoryMock!.Setup(repo => repo.GetAllCategoriesAsync()).ReturnsAsync(cats);

            var result = await _categoryService!.DeleteCategoryAsync(catId);

            result.Should().BeTrue();
            _categoryRepositoryMock.Verify(repo => repo.DeleteCategoryAsync(It.IsAny<Category>()), Times.Once);
        }

        // [TEST 8]: Find category by its unique slug.
        [TestMethod]
        public async Task GetCategoryBySlugAsync_ShouldReturnCategory()
        {
            // Logic: Navigation check. Users look up categories by name-based URLs.
            var cat = new Category { Name = "Health", Slug = "health" };
            _categoryRepositoryMock!.Setup(repo => repo.GetCategoryBySlugAsync("health")).ReturnsAsync(cat);

            var result = await _categoryService!.GetCategoryBySlugAsync("health");

            result!.Name.Should().Be("Health");
        }

        // [TEST 9]: Test collision fallback in CreateTagAsync.
        [TestMethod]
        public async Task CreateTagAsync_ShouldUseFallback_OnCollision()
        {
            // Logic: Try-Catch safety check. If a collision happens, force a unique suffix.
            _categoryRepositoryMock!.Setup(repo => repo.TagSlugExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
            _categoryRepositoryMock.SetupSequence(repo => repo.CreateTagAsync(It.IsAny<Tag>()))
                .ThrowsAsync(new Exception("Slug collision")) // First attempt fails
                .ReturnsAsync(new Tag { Slug = "tag-abcd" }); // Second attempt succeeds

            var result = await _categoryService!.CreateTagAsync(new CreateTagDTO { Name = "Tag" });

            result.Slug.Should().Contain("tag");
        }

        // [TEST 10]: Create a simple tag.
        [TestMethod]
        public async Task CreateTagAsync_ShouldReturnTagResponse()
        {
            // Logic: Basic functionality check for tags.
            var dto = new CreateTagDTO { Name = "CSharp" };
            _categoryRepositoryMock!.Setup(repo => repo.TagSlugExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
            _categoryRepositoryMock.Setup(repo => repo.CreateTagAsync(It.IsAny<Tag>())).ReturnsAsync((Tag t) => t);

            var result = await _categoryService!.CreateTagAsync(dto);

            result.Name.Should().Be("CSharp");
            result.Slug.Should().Be("csharp");
        }

        // [TEST 11]: Get all categories list.
        [TestMethod]
        public async Task GetAllCategoriesAsync_ShouldReturnAll()
        {
            // Logic: Basic retrieval check.
            var cats = new List<Category> { new Category { Name = "C1" }, new Category { Name = "C2" } };
            _categoryRepositoryMock!.Setup(repo => repo.GetAllCategoriesAsync()).ReturnsAsync(cats);

            var result = await _categoryService!.GetAllCategoriesAsync();

            result.Count().Should().Be(2);
        }
    }
}
