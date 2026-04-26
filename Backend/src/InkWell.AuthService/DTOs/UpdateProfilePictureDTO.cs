using System.ComponentModel.DataAnnotations;

namespace InkWell.AuthService.DTOs
{
    public class UpdateProfilePictureDTO
    {
        [Required]
        [Url]
        public string Url { get; set; } = string.Empty;
    }
}
