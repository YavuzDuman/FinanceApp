using Microsoft.EntityFrameworkCore;
using UserService.DataAccess.Abstract;
using UserService.DataAccess.Context;
using UserService.Entities.Concrete;

namespace UserService.DataAccess.Concrete
{
    public class UserRoleRepository : EfRepository<UserRole>, IUserRoleRepository
    {
        private readonly UserDatabaseContext _context;

        public UserRoleRepository(UserDatabaseContext context) : base(context)
        {
            _context = context;
        }

        public async Task<List<UserRole>> GetUserRolesAsync(int userId, CancellationToken ct = default)
        {
            return await _context.UserRoles
                .Include(ur => ur.Role)
                .Where(ur => ur.UserId == userId)
                .ToListAsync(ct);
        }

        public async Task<bool> UserHasRoleAsync(int userId, string roleName, CancellationToken ct = default)
        {
            return await _context.UserRoles
                .Include(ur => ur.Role)
                .AnyAsync(ur => ur.UserId == userId && ur.Role.Name == roleName, ct);
        }

        public async Task AddRoleToUserAsync(int userId, int roleId, CancellationToken ct = default)
        {
            var userRole = new UserRole
            {
                UserId = userId,
                RoleId = roleId
            };

            await _context.UserRoles.AddAsync(userRole, ct);
            await _context.SaveChangesAsync(ct);
        }

        public async Task RemoveRoleFromUserAsync(int userId, int roleId, CancellationToken ct = default)
        {
            var userRole = await _context.UserRoles
                .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId, ct);

            if (userRole != null)
            {
                _context.UserRoles.Remove(userRole);
                await _context.SaveChangesAsync(ct);
            }
        }
    }
}
