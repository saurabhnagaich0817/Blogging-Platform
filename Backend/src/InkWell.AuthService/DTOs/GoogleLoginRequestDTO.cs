using System.ComponentModel.DataAnnotations;

namespace InkWell.AuthService.DTOs
{
    public class GoogleLoginRequestDTO
    {
        [Required]
        public string Token { get; set; } = string.Empty;
    }
}
