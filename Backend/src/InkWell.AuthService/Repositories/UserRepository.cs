using InkWell.AuthService.Data;
using InkWell.AuthService.Models;
using Microsoft.EntityFrameworkCore;

namespace InkWell.AuthService.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly AuthDbContext _context;

        public UserRepository(AuthDbContext context)
        {
            _context = context;
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            // Includes the Roles so we can use them for JWT generation
            return await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User> CreateUserAsync(User user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task AssignRoleAsync(Guid userId, int roleId)
        {
            var userRole = new UserRole { UserId = userId, RoleId = roleId };
            _context.UserRoles.Add(userRole);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveAllRolesAsync(Guid userId)
        {
            var userRoles = await _context.UserRoles.Where(ur => ur.UserId == userId).ToListAsync();
            _context.UserRoles.RemoveRange(userRoles);
            await _context.SaveChangesAsync();
        }

        public async Task<Role?> GetRoleByNameAsync(string roleName)
        {
            return await _context.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
        }

        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            return await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .ToListAsync();
        }

        public async Task<User?> GetByUsernameAsync(string username)
        {
            return await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Username == username);
        }

        public async Task<User?> GetByIdAsync(Guid userId)
        {
            return await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == userId);
        }

        public async Task UpdateUserAsync(User user)
        {
            _context.Entry(user).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        public async Task<Connection?> GetConnectionAsync(Guid requesterId, Guid receiverId)
        {
            return await _context.Connections
                .FirstOrDefaultAsync(c => (c.RequesterId == requesterId && c.ReceiverId == receiverId) || 
                                          (c.RequesterId == receiverId && c.ReceiverId == requesterId));
        }

        public async Task CreateConnectionAsync(Connection connection)
        {
            _context.Connections.Add(connection);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateConnectionAsync(Connection connection)
        {
            _context.Entry(connection).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<Connection>> GetUserConnectionsAsync(Guid userId)
        {
            return await _context.Connections
                .Where(c => c.RequesterId == userId || c.ReceiverId == userId)
                .Include(c => c.Requester)
                .Include(c => c.Receiver)
                .ToListAsync();
        }
        public async Task DeleteUserAsync(Guid userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                // Related entities (Roles, Connections) will be deleted if Cascade is on, 
                // but let's be explicit if needed.
                var roles = await _context.UserRoles.Where(ur => ur.UserId == userId).ToListAsync();
                _context.UserRoles.RemoveRange(roles);

                var connections = await _context.Connections.Where(c => c.RequesterId == userId || c.ReceiverId == userId).ToListAsync();
                _context.Connections.RemoveRange(connections);

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
            }
        }
    }
}
