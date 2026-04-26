using System.ComponentModel.DataAnnotations;

namespace InkWell.AuthService.DTOs
{
    // DTO for capturing login credentials
    public class LoginRequestDTO
    {
        [Required]
        [EmailAddress]
        public string? Email { get; set; }

        [Required]
        public string? Password { get; set; }
    }
}
