namespace InkWell.AuthService.DTOs
{
    // Response sent back to client after auth operations
    public class AuthResponseDTO
    {
        public string? Token { get; set; }
        public string? Message { get; set; }
        public bool IsSuccess { get; set; }
        public bool IsNewUser { get; set; }
    }
}
