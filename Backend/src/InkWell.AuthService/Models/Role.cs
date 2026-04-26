namespace InkWell.AuthService.Models
{
    public class Role
    {
        public int Id { get; set; } // Primary Key
        public string Name { get; set; } = string.Empty; // e.g. Admin, Author, Reader
        
        // Navigation Property: A role can be assigned to many users
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }
}
