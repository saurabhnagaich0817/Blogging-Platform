using InkWell.MediaService.DTOs;
using InkWell.MediaService.Models;
using InkWell.MediaService.Repositories;

namespace InkWell.MediaService.Services
{
    /// <summary>
    /// Service responsible for handling media uploads, storage abstraction, 
    /// and binary data management.
    /// </summary>
    public class MediaService : IMediaService
    {
        private readonly IMediaRepository _repository;
        private readonly IFileStorageService _fileStorage;

        public MediaService(IMediaRepository repository, IFileStorageService fileStorage)
        {
            _repository = repository;
            _fileStorage = fileStorage; // Clean Architecture: Inject the abstraction
        }

        /// <summary>
        /// Validates and uploads a file to the storage system and records metadata in the database.
        /// </summary>
        public async Task<MediaResponseDTO> UploadMediaAsync(UploadMediaDTO dto, Guid uploaderId)
        {
            // Business Logic: Validate File Size (10MB Max)
            if (dto.File.Length > 10 * 1024 * 1024)
            {
                throw new ArgumentException("File size exceeds 10MB limit.");
            }

            // Business Logic: Validate File Type
            var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp" };
            if (!allowedTypes.Contains(dto.File.ContentType.ToLower()))
            {
                throw new ArgumentException("Only JPG, PNG, and WEBP files are allowed.");
            }

            // Save using Storage Service (Local or S3 depending on injection)
            var (fileName, url) = await _fileStorage.SaveFileAsync(dto.File);

            var media = new Media
            {
                MediaId = Guid.NewGuid(),
                UploaderId = uploaderId,
                FileName = fileName,
                OriginalName = dto.File.FileName,
                Url = url,
                MimeType = dto.File.ContentType,
                SizeKb = dto.File.Length / 1024,
                AltText = dto.AltText?.Trim() ?? string.Empty,
                LinkedPostId = dto.LinkedPostId,
                UploadedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            var savedMedia = await _repository.UploadMediaAsync(media);
            return MapToDTO(savedMedia);
        }

        public async Task<MediaResponseDTO?> GetMediaByIdAsync(Guid id)
        {
            var media = await _repository.GetMediaByIdAsync(id);
            return media == null ? null : MapToDTO(media);
        }

        public async Task<IEnumerable<MediaResponseDTO>> GetMediaByPostAsync(Guid postId)
        {
            var mediaItems = await _repository.GetMediaByPostAsync(postId);
            return mediaItems.Select(MapToDTO);
        }

        public async Task<IEnumerable<MediaResponseDTO>> GetMediaByUserAsync(Guid userId)
        {
            var mediaItems = await _repository.GetMediaByUserAsync(userId);
            return mediaItems.Select(MapToDTO);
        }

        public async Task<IEnumerable<MediaResponseDTO>> GetAllMediaAsync()
        {
            var mediaItems = await _repository.GetAllMediaAsync();
            return mediaItems.Select(MapToDTO);
        }

        public async Task<bool> DeleteMediaAsync(Guid id, Guid uploaderId, string role)
        {
            var media = await _repository.GetMediaByIdAsync(id);
            if (media == null) return false;

            if (media.UploaderId != uploaderId && role != "Admin") return false;

            // Soft delete in database
            media.IsDeleted = true;
            await _repository.UpdateMediaAsync(media);
            
            // Note: We don't delete the physical file from S3/Local on soft delete.
            // If hard delete is required later, uncomment: await _fileStorage.DeleteFileAsync(media.FileName);

            return true;
        }

        public async Task<MediaResponseDTO?> UpdateAltTextAsync(Guid id, string altText, Guid uploaderId)
        {
            var media = await _repository.GetMediaByIdAsync(id);
            if (media == null || media.UploaderId != uploaderId) return null;

            media.AltText = altText?.Trim() ?? string.Empty;
            await _repository.UpdateMediaAsync(media);

            return MapToDTO(media);
        }

        private MediaResponseDTO MapToDTO(Media m) => new MediaResponseDTO
        {
            MediaId = m.MediaId, UploaderId = m.UploaderId, OriginalName = m.OriginalName,
            Url = m.Url, MimeType = m.MimeType, SizeKb = m.SizeKb, AltText = m.AltText,
            LinkedPostId = m.LinkedPostId, UploadedAt = m.UploadedAt
        };
    }
}
