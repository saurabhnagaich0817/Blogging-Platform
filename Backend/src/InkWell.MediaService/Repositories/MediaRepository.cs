using InkWell.MediaService.Data;
using InkWell.MediaService.Models;
using Microsoft.EntityFrameworkCore;

namespace InkWell.MediaService.Repositories
{
    public class MediaRepository : IMediaRepository
    {
        private readonly MediaDbContext _context;

        public MediaRepository(MediaDbContext context)
        {
            _context = context;
        }

        public async Task<Media> UploadMediaAsync(Media media)
        {
            _context.MediaItems.Add(media);
            await _context.SaveChangesAsync();
            return media;
        }

        public async Task<Media?> GetMediaByIdAsync(Guid id)
        {
            return await _context.MediaItems.FindAsync(id);
        }

        public async Task<IEnumerable<Media>> GetMediaByPostAsync(Guid postId)
        {
            return await _context.MediaItems
                .Where(m => m.LinkedPostId == postId)
                .OrderByDescending(m => m.UploadedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Media>> GetMediaByUserAsync(Guid userId)
        {
            return await _context.MediaItems
                .Where(m => m.UploaderId == userId)
                .OrderByDescending(m => m.UploadedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Media>> GetAllMediaAsync()
        {
            return await _context.MediaItems
                .OrderByDescending(m => m.UploadedAt)
                .ToListAsync();
        }

        public async Task UpdateMediaAsync(Media media)
        {
            _context.MediaItems.Update(media);
            await _context.SaveChangesAsync();
        }
    }
}
