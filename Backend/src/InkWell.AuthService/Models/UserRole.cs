namespace InkWell.AuthService.Models
{
    // Junction table representing many-to-many relationship between User and Role
    public class UserRole
    {
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        public int RoleId { get; set; }
        public Role Role { get; set; } = null!;
    }
}
