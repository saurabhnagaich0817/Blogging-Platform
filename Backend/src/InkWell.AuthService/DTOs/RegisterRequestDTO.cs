using System.ComponentModel.DataAnnotations;

namespace InkWell.AuthService.DTOs
{
    // DTO for capturing data from client when they sign up
    public class RegisterRequestDTO
    {
        [Required]
        [StringLength(100)]
        public string? FullName { get; set; }

        [Required]
        [StringLength(50, MinimumLength = 3)]
        public string? Username { get; set; }

        [Required]
        [EmailAddress]
        public string? Email { get; set; }

        [Required]
        [MinLength(6)]
        public string? Password { get; set; }

        public string? Role { get; set; } = "Reader";
    }
}
