using System.ComponentModel.DataAnnotations;

namespace InkWell.AuthService.DTOs
{
    public class UpgradeRequestDTO
    {
        [Required]
        public string Role { get; set; } = "Author";
    }
}
