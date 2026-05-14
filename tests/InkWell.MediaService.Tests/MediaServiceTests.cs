using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using FluentAssertions;
using InkWell.MediaService.Services;
using InkWell.MediaService.Repositories;
using InkWell.MediaService.DTOs;
using InkWell.MediaService.Models;
using Microsoft.AspNetCore.Http;

namespace InkWell.MediaService.Tests
{
    [TestClass]
    public class MediaServiceTests
    {
        private Mock<IMediaRepository>? _mediaRepositoryMock;
        private Mock<IFileStorageService>? _fileStorageMock;
        private InkWell.MediaService.Services.MediaService? _mediaService;

        [TestInitialize]
        public void Setup()
        {
            _mediaRepositoryMock = new Mock<IMediaRepository>();
            _fileStorageMock = new Mock<IFileStorageService>();
            _mediaService = new InkWell.MediaService.Services.MediaService(
                _mediaRepositoryMock.Object, 
                _fileStorageMock.Object);
        }

        // [TEST 1]: Successful file upload.
        [TestMethod]
        public async Task UploadMediaAsync_ShouldReturnMediaDetails_WhenSuccessful()
        {
            var uploaderId = Guid.NewGuid();
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.Length).Returns(1024);
            fileMock.Setup(f => f.ContentType).Returns("image/jpeg");
            fileMock.Setup(f => f.FileName).Returns("test.jpg");

            var dto = new UploadMediaDTO { File = fileMock.Object, AltText = "Alt" };
            _fileStorageMock!.Setup(fs => fs.SaveFileAsync(It.IsAny<IFormFile>())).ReturnsAsync(("f.jpg", "http://u.com/f.jpg"));
            _mediaRepositoryMock!.Setup(repo => repo.UploadMediaAsync(It.IsAny<Media>())).ReturnsAsync((Media m) => m);

            var result = await _mediaService!.UploadMediaAsync(dto, uploaderId);

            result.Url.Should().Be("http://u.com/f.jpg");
            _mediaRepositoryMock.Verify(repo => repo.UploadMediaAsync(It.IsAny<Media>()), Times.Once);
        }

        // [TEST 2]: Enforce 10MB size limit.
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task UploadMediaAsync_ShouldThrowException_WhenFileSizeExceeds10MB()
        {
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.Length).Returns(11 * 1024 * 1024); // 11MB
            var dto = new UploadMediaDTO { File = fileMock.Object };

            await _mediaService!.UploadMediaAsync(dto, Guid.NewGuid());
        }

        // [TEST 3]: Validate file extensions (Security).
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task UploadMediaAsync_ShouldThrowException_WhenFileTypeIsInvalid()
        {
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.Length).Returns(1024);
            fileMock.Setup(f => f.ContentType).Returns("application/pdf"); // Not allowed
            var dto = new UploadMediaDTO { File = fileMock.Object };

            await _mediaService!.UploadMediaAsync(dto, Guid.NewGuid());
        }

        // [TEST 4]: Soft delete by owner.
        [TestMethod]
        public async Task DeleteMediaAsync_ShouldPerformSoftDelete_WhenUserIsOwner()
        {
            var mediaId = Guid.NewGuid();
            var uploaderId = Guid.NewGuid();
            var media = new Media { MediaId = mediaId, UploaderId = uploaderId, IsDeleted = false };
            _mediaRepositoryMock!.Setup(repo => repo.GetMediaByIdAsync(mediaId)).ReturnsAsync(media);

            await _mediaService!.DeleteMediaAsync(mediaId, uploaderId, "Author");

            media.IsDeleted.Should().BeTrue();
            _mediaRepositoryMock.Verify(repo => repo.UpdateMediaAsync(media), Times.Once);
        }

        // [TEST 5]: Admin can soft delete any file.
        [TestMethod]
        public async Task DeleteMediaAsync_ShouldAllowAdminToDelete()
        {
            var mediaId = Guid.NewGuid();
            var media = new Media { MediaId = mediaId, UploaderId = Guid.NewGuid() };
            _mediaRepositoryMock!.Setup(repo => repo.GetMediaByIdAsync(mediaId)).ReturnsAsync(media);

            var result = await _mediaService!.DeleteMediaAsync(mediaId, Guid.NewGuid(), "Admin");

            result.Should().BeTrue();
            media.IsDeleted.Should().BeTrue();
        }

        // [TEST 6]: Prevent unauthorized deletion.
        [TestMethod]
        public async Task DeleteMediaAsync_ShouldReturnFalse_WhenUserIsNotOwner()
        {
            var mediaId = Guid.NewGuid();
            var media = new Media { MediaId = mediaId, UploaderId = Guid.NewGuid() };
            _mediaRepositoryMock!.Setup(repo => repo.GetMediaByIdAsync(mediaId)).ReturnsAsync(media);

            var result = await _mediaService!.DeleteMediaAsync(mediaId, Guid.NewGuid(), "Reader");

            result.Should().BeFalse();
        }

        // [TEST 7]: Handle null alt text gracefully.
        [TestMethod]
        public async Task UploadMediaAsync_ShouldHandleNullAltText()
        {
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.Length).Returns(1024);
            fileMock.Setup(f => f.ContentType).Returns("image/png");
            var dto = new UploadMediaDTO { File = fileMock.Object, AltText = null };

            _fileStorageMock!.Setup(fs => fs.SaveFileAsync(It.IsAny<IFormFile>())).ReturnsAsync(("f.png", "url"));
            _mediaRepositoryMock!.Setup(repo => repo.UploadMediaAsync(It.IsAny<Media>())).ReturnsAsync((Media m) => m);

            var result = await _mediaService!.UploadMediaAsync(dto, Guid.NewGuid());

            result.AltText.Should().Be("");
        }

        // [TEST 8]: Fetch media by post ID.
        [TestMethod]
        public async Task GetMediaByPostAsync_ShouldReturnList()
        {
            var postId = Guid.NewGuid();
            var items = new List<Media> { new Media { LinkedPostId = postId } };
            _mediaRepositoryMock!.Setup(repo => repo.GetMediaByPostAsync(postId)).ReturnsAsync(items);

            var result = await _mediaService!.GetMediaByPostAsync(postId);

            result.Count().Should().Be(1);
        }

        // [TEST 9]: Fetch media by user ID.
        [TestMethod]
        public async Task GetMediaByUserAsync_ShouldReturnList()
        {
            var userId = Guid.NewGuid();
            var items = new List<Media> { new Media { UploaderId = userId } };
            _mediaRepositoryMock!.Setup(repo => repo.GetMediaByUserAsync(userId)).ReturnsAsync(items);

            var result = await _mediaService!.GetMediaByUserAsync(userId);

            result.Count().Should().Be(1);
        }

        // [TEST 10]: Update Alt text success.
        [TestMethod]
        public async Task UpdateAltTextAsync_ShouldSucceed_WhenOwner()
        {
            var mediaId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var media = new Media { MediaId = mediaId, UploaderId = userId, AltText = "Old" };
            _mediaRepositoryMock!.Setup(repo => repo.GetMediaByIdAsync(mediaId)).ReturnsAsync(media);

            var result = await _mediaService!.UpdateAltTextAsync(mediaId, "New", userId);

            result!.AltText.Should().Be("New");
        }

        // [TEST 11]: Fail Alt text update if not owner.
        [TestMethod]
        public async Task UpdateAltTextAsync_ShouldReturnNull_WhenNotOwner()
        {
            var mediaId = Guid.NewGuid();
            var media = new Media { MediaId = mediaId, UploaderId = Guid.NewGuid() };
            _mediaRepositoryMock!.Setup(repo => repo.GetMediaByIdAsync(mediaId)).ReturnsAsync(media);

            var result = await _mediaService!.UpdateAltTextAsync(mediaId, "New", Guid.NewGuid());

            result.Should().BeNull();
        }

        // [TEST 12]: Correct Size calculation.
        [TestMethod]
        public async Task UploadMediaAsync_ShouldCalculateSizeInKb()
        {
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.Length).Returns(2048); // 2KB
            fileMock.Setup(f => f.ContentType).Returns("image/webp");
            var dto = new UploadMediaDTO { File = fileMock.Object };

            _fileStorageMock!.Setup(fs => fs.SaveFileAsync(It.IsAny<IFormFile>())).ReturnsAsync(("f.webp", "url"));
            _mediaRepositoryMock!.Setup(repo => repo.UploadMediaAsync(It.IsAny<Media>())).ReturnsAsync((Media m) => m);

            var result = await _mediaService!.UploadMediaAsync(dto, Guid.NewGuid());

            result.SizeKb.Should().Be(2);
        }

        // [TEST 13]: Get all media items.
        [TestMethod]
        public async Task GetAllMediaAsync_ShouldReturnAll()
        {
            _mediaRepositoryMock!.Setup(repo => repo.GetAllMediaAsync()).ReturnsAsync(new List<Media> { new Media(), new Media() });
            var result = await _mediaService!.GetAllMediaAsync();
            result.Count().Should().Be(2);
        }

        // [TEST 14]: Get single media by ID.
        [TestMethod]
        public async Task GetMediaByIdAsync_ShouldReturnMedia()
        {
            var id = Guid.NewGuid();
            _mediaRepositoryMock!.Setup(repo => repo.GetMediaByIdAsync(id)).ReturnsAsync(new Media { MediaId = id });
            var result = await _mediaService!.GetMediaByIdAsync(id);
            result.Should().NotBeNull();
        }
    }
}
