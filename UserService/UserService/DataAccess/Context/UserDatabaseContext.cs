using Microsoft.EntityFrameworkCore;
using UserService.Entities.Concrete;

namespace UserService.DataAccess.Context
{
	public class UserDatabaseContext : DbContext
	{
		public UserDatabaseContext(DbContextOptions<UserDatabaseContext> options) : base(options)
		{
		}
		public DbSet<User> Users { get; set; }
		public DbSet<Role> Roles { get; set; }
		public DbSet<UserRole> UserRoles { get; set; }
		public DbSet<RefreshToken> RefreshTokens { get; set; }

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			//modelBuilder.Entity<UserDto>().HasNoKey();
			base.OnModelCreating(modelBuilder);

			modelBuilder.Entity<UserRole>()
				.HasKey(ur => new { ur.UserId, ur.RoleId });

			modelBuilder.Entity<UserRole>()
				.HasOne(ur => ur.User)
				.WithMany(u => u.UserRoles)
				.HasForeignKey(ur => ur.UserId);

		modelBuilder.Entity<UserRole>()
			.HasOne(ur => ur.Role)
			.WithMany(r => r.UserRoles)
			.HasForeignKey(ur => ur.RoleId);

		// RefreshToken - User ilişkisi
		modelBuilder.Entity<RefreshToken>()
			.HasOne(rt => rt.User)
			.WithMany()
			.HasForeignKey(rt => rt.UserId)
			.OnDelete(DeleteBehavior.Cascade);
	}
}
}
