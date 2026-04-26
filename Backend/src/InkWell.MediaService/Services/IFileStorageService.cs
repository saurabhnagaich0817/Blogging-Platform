using Microsoft.AspNetCore.Http;

namespace InkWell.MediaService.Services
{
    /// <summary>
    /// Abstraction for File Storage. 
    /// Keeps the architecture clean so we can easily swap to AWS S3, Azure Blob, or GCP Storage later.
    /// </summary>
    public interface IFileStorageService
    {
        Task<(string FileName, string Url)> SaveFileAsync(IFormFile file);
        Task DeleteFileAsync(string fileName);
    }
}
