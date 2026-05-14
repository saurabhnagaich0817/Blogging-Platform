using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using FluentAssertions;
using InkWell.CommentService.Services;
using InkWell.CommentService.Repositories;
using InkWell.CommentService.DTOs;
using InkWell.CommentService.Models;
using MassTransit;
using InkWell.Shared.Events;

namespace InkWell.CommentService.Tests
{
    [TestClass]
    public class CommentServiceTests
    {
        private Mock<ICommentRepository>? _commentRepositoryMock;
        private Mock<IPublishEndpoint>? _publishEndpointMock;
        private InkWell.CommentService.Services.CommentService? _commentService;

        [TestInitialize]
        public void Setup()
        {
            _commentRepositoryMock = new Mock<ICommentRepository>();
            _publishEndpointMock = new Mock<IPublishEndpoint>();

            _commentService = new InkWell.CommentService.Services.CommentService(
                _commentRepositoryMock.Object, 
                _publishEndpointMock.Object);
        }

        // [TEST 1]: Add comment and verify notification event is published.
        [TestMethod]
        public async Task AddCommentAsync_ShouldPublishEvent_WhenCommentedOnOtherUsersPost()
        {
            var authorId = Guid.NewGuid();
            var postAuthorId = Guid.NewGuid();
            var dto = new CreateCommentDTO { PostId = Guid.NewGuid(), Content = "Great post!", PostAuthorId = postAuthorId, PostAuthorEmail = "a@t.com" };

            _commentRepositoryMock!.Setup(repo => repo.AddCommentAsync(It.IsAny<Comment>())).ReturnsAsync((Comment c) => c);

            await _commentService!.AddCommentAsync(dto, authorId, "Commenter");

            _publishEndpointMock!.Verify(p => p.Publish(It.IsAny<CommentAddedEvent>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        // [TEST 2]: Ensure no notification is sent if user comments on their own post.
        [TestMethod]
        public async Task AddCommentAsync_ShouldNotPublishEvent_WhenAuthorIsPostAuthor()
        {
            var authorId = Guid.NewGuid();
            var dto = new CreateCommentDTO { PostId = Guid.NewGuid(), Content = "Self comment", PostAuthorId = authorId };

            _commentRepositoryMock!.Setup(repo => repo.AddCommentAsync(It.IsAny<Comment>())).ReturnsAsync((Comment c) => c);

            await _commentService!.AddCommentAsync(dto, authorId, "Me");

            _publishEndpointMock!.Verify(p => p.Publish(It.IsAny<CommentAddedEvent>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        // [TEST 3]: Verify soft delete logic (Status changes to 'Deleted').
        [TestMethod]
        public async Task DeleteCommentAsync_ShouldPerformSoftDelete()
        {
            var commentId = Guid.NewGuid();
            var authorId = Guid.NewGuid();
            var comment = new Comment { CommentId = commentId, AuthorId = authorId, Status = "Approved" };

            _commentRepositoryMock!.Setup(repo => repo.GetCommentByIdAsync(commentId)).ReturnsAsync(comment);

            await _commentService!.DeleteCommentAsync(commentId, authorId, "Reader");

            comment.Status.Should().Be("Deleted");
            _commentRepositoryMock.Verify(repo => repo.DeleteCommentAsync(It.IsAny<Comment>()), Times.Once);
        }

        // [TEST 4]: Admin should be able to delete any user's comment.
        [TestMethod]
        public async Task DeleteCommentAsync_ShouldAllowAdminToDelete()
        {
            var commentId = Guid.NewGuid();
            var comment = new Comment { CommentId = commentId, AuthorId = Guid.NewGuid() };

            _commentRepositoryMock!.Setup(repo => repo.GetCommentByIdAsync(commentId)).ReturnsAsync(comment);

            var result = await _commentService!.DeleteCommentAsync(commentId, Guid.NewGuid(), "Admin");

            result.Should().BeTrue();
            comment.Status.Should().Be("Deleted");
        }

        // [TEST 5]: Update comment content if user is the author.
        [TestMethod]
        public async Task UpdateCommentAsync_ShouldSucceed_WhenUserIsAuthor()
        {
            var commentId = Guid.NewGuid();
            var authorId = Guid.NewGuid();
            var comment = new Comment { CommentId = commentId, AuthorId = authorId, Content = "Old" };

            _commentRepositoryMock!.Setup(repo => repo.GetCommentByIdAsync(commentId)).ReturnsAsync(comment);
            _commentRepositoryMock.Setup(repo => repo.UpdateCommentAsync(It.IsAny<Comment>())).ReturnsAsync((Comment c) => c);

            var result = await _commentService!.UpdateCommentAsync(commentId, new UpdateCommentDTO { Content = "New" }, authorId);

            result!.Content.Should().Be("New");
        }

        // [TEST 6]: Prevent editing of other users' comments.
        [TestMethod]
        public async Task UpdateCommentAsync_ShouldFail_WhenUserIsNotAuthor()
        {
            var commentId = Guid.NewGuid();
            var comment = new Comment { CommentId = commentId, AuthorId = Guid.NewGuid() };
            _commentRepositoryMock!.Setup(repo => repo.GetCommentByIdAsync(commentId)).ReturnsAsync(comment);

            var result = await _commentService!.UpdateCommentAsync(commentId, new UpdateCommentDTO { Content = "Hacked" }, Guid.NewGuid());

            result.Should().BeNull();
        }

        // [TEST 7]: Increase likes count.
        [TestMethod]
        public async Task LikeCommentAsync_ShouldIncreaseLikes()
        {
            var commentId = Guid.NewGuid();
            var comment = new Comment { CommentId = commentId, LikesCount = 5 };
            _commentRepositoryMock!.Setup(repo => repo.GetCommentByIdAsync(commentId)).ReturnsAsync(comment);

            await _commentService!.LikeCommentAsync(commentId);

            comment.LikesCount.Should().Be(6);
        }

        // [TEST 8]: Decrease likes count but stop at zero.
        [TestMethod]
        public async Task UnlikeCommentAsync_ShouldNotGoBelowZero()
        {
            var commentId = Guid.NewGuid();
            var comment = new Comment { CommentId = commentId, LikesCount = 0 };
            _commentRepositoryMock!.Setup(repo => repo.GetCommentByIdAsync(commentId)).ReturnsAsync(comment);

            var result = await _commentService!.UnlikeCommentAsync(commentId);

            result.Should().BeFalse();
            comment.LikesCount.Should().Be(0);
        }

        // [TEST 9]: Fetch all comments for a post.
        [TestMethod]
        public async Task GetCommentsByPostAsync_ShouldReturnList()
        {
            var postId = Guid.NewGuid();
            var comments = new List<Comment> { new Comment { PostId = postId }, new Comment { PostId = postId } };
            _commentRepositoryMock!.Setup(repo => repo.GetCommentsByPostAsync(postId)).ReturnsAsync(comments);

            var result = await _commentService!.GetCommentsByPostAsync(postId);

            result.Count().Should().Be(2);
        }

        // [TEST 10]: Verify reply retrieval logic.
        [TestMethod]
        public async Task GetRepliesAsync_ShouldReturnReplies()
        {
            var parentId = Guid.NewGuid();
            var replies = new List<Comment> { new Comment { ParentCommentId = parentId } };
            _commentRepositoryMock!.Setup(repo => repo.GetRepliesAsync(parentId)).ReturnsAsync(replies);

            var result = await _commentService!.GetRepliesAsync(parentId);

            result.Count().Should().Be(1);
        }

        // [TEST 11]: Admin moderation (Approve).
        [TestMethod]
        public async Task ApproveCommentAsync_ShouldSetStatusToApproved()
        {
            var commentId = Guid.NewGuid();
            var comment = new Comment { CommentId = commentId, Status = "Pending" };
            _commentRepositoryMock!.Setup(repo => repo.GetCommentByIdAsync(commentId)).ReturnsAsync(comment);

            await _commentService!.ApproveCommentAsync(commentId);

            comment.Status.Should().Be("Approved");
        }
    }
}
