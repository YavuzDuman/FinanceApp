using Microsoft.EntityFrameworkCore;
using UserService.DataAccess.Abstract;
using UserService.DataAccess.Context;
using UserService.Entities.Concrete;

namespace UserService.DataAccess.Concrete
{
    public class RoleRepository : EfRepository<Role>, IRoleRepository
    {
        private readonly UserDatabaseContext _context;

        public RoleRepository(UserDatabaseContext context) : base(context)
        {
            _context = context;
        }

        public async Task<Role?> GetRoleByNameAsync(string roleName, CancellationToken ct = default)
        {
            return await _context.Roles
                .FirstOrDefaultAsync(r => r.Name == roleName, ct);
        }

        public async Task<List<Role>> GetAllRolesAsync(CancellationToken ct = default)
        {
            return await _context.Roles.ToListAsync(ct);
        }

        public async Task<bool> RoleExistsAsync(string roleName, CancellationToken ct = default)
        {
            return await _context.Roles.AnyAsync(r => r.Name == roleName, ct);
        }
    }
}
