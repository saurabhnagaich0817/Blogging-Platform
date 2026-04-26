using InkWell.AuthService.Models;

namespace InkWell.AuthService.Repositories
{
    public interface IUserRepository
    {
        Task<User?> GetByEmailAsync(string email);
        Task<User> CreateUserAsync(User user);
        Task AssignRoleAsync(Guid userId, int roleId);
        Task RemoveAllRolesAsync(Guid userId);
        Task<Role?> GetRoleByNameAsync(string roleName);
        Task<IEnumerable<User>> GetAllUsersAsync();
        Task<User?> GetByUsernameAsync(string username);
        Task<User?> GetByIdAsync(Guid userId);
        Task UpdateUserAsync(User user);
        
        // Connections
        Task<Connection?> GetConnectionAsync(Guid requesterId, Guid receiverId);
        Task CreateConnectionAsync(Connection connection);
        Task UpdateConnectionAsync(Connection connection);
        Task<IEnumerable<Connection>> GetUserConnectionsAsync(Guid userId);
        Task DeleteUserAsync(Guid userId);
    }
}
