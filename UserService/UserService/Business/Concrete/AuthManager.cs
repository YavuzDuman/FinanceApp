using AutoMapper;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using UserService.Business.Abstract;
using UserService.DataAccess.Abstract;
using UserService.DataAccess.Context;
using UserService.Entities.Concrete;
using UserService.Entities.Dto;
using UserService.Helpers.Hashing;

namespace UserService.Business.Concrete
{
	public class AuthManager : IAuthManager
	{
		private readonly IAuthRepository _repo;
		private readonly PasswordHasher _passwordHasher; 
		private readonly IMapper _mapper;
		private readonly UserDatabaseContext _context;
		private readonly IRoleRepository _roleRepository;
		private readonly IUserRoleRepository _userRoleRepository;

		public AuthManager(IAuthRepository repo, PasswordHasher passwordHasher, IMapper mapper, UserDatabaseContext context, IRoleRepository roleRepository, IUserRoleRepository userRoleRepository)
		{
			_repo = repo;
			_passwordHasher = passwordHasher;
			_mapper = mapper;
			_context = context;
			_roleRepository = roleRepository;
			_userRoleRepository = userRoleRepository;
		}

		public Task<User?> LoginUserAsync(LoginDto loginUser, CancellationToken ct = default)
		{
			var user = _mapper.Map<User>(loginUser); 
			return _repo.LoginUserAsync(user, ct);
		}
		 

	public async Task<bool> RegisterUserAsync(RegisterDto dto, CancellationToken ct = default)
	{
		var userToRegister = _mapper.Map<User>(dto);
		var exists = await _repo.ExistsByUsernameOrEmailAsync(userToRegister.Username, userToRegister.Email, ct);
		if (exists) return false;
		
		var registeredUser = await _repo.RegisterUserAsync(userToRegister, ct);
		
		// Yeni kullanıcıya otomatik olarak "User" rolünü ata
		try
		{
			var userRole = await _roleRepository.GetRoleByNameAsync("User", ct);
			if (userRole != null)
			{
				// Aynı context üzerinden rol atama (transaction içinde)
				var userRoleEntity = new UserRole
				{
					UserId = registeredUser.UserId,
					RoleId = userRole.Id
				};
				
				_context.UserRoles.Add(userRoleEntity);
				await _context.SaveChangesAsync(ct);
				
				Console.WriteLine($"Kullanıcı {registeredUser.UserId} başarıyla 'User' rolüne atandı.");
			}
			else
			{
				Console.WriteLine("UYARI: 'User' rolü bulunamadı. Kullanıcı rolü olmadan kaydedildi.");
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"UYARI: Rol atama hatası: {ex.Message}");
			Console.WriteLine($"Stack trace: {ex.StackTrace}");
			if (ex.InnerException != null)
			{
				Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
			}
			// Rol atama başarısız olsa bile kullanıcı kaydı tamamlanmış, devam et
		}
		
		return true;
	}

		public Task<User?> GetUserByIdAsync(int userId, CancellationToken ct = default)
			=> _repo.GetUserByIdAsync(userId, ct);

		// ==================== REFRESH TOKEN METODLARI ====================

		public async Task<RefreshToken> CreateRefreshTokenAsync(int userId, string ipAddress, CancellationToken ct = default)
		{
			// Güvenli rastgele token oluştur
			var randomBytes = new byte[64];
			using (var rng = RandomNumberGenerator.Create())
			{
				rng.GetBytes(randomBytes);
			}
			var token = Convert.ToBase64String(randomBytes);

			var refreshToken = new RefreshToken
			{
				UserId = userId,
				Token = token,
				ExpiryDate = DateTime.UtcNow.AddDays(7), // 7 gün geçerli
				CreatedDate = DateTime.UtcNow,
				IsRevoked = false
			};

			_context.RefreshTokens.Add(refreshToken);
			await _context.SaveChangesAsync(ct);

			return refreshToken;
		}

		public async Task<User?> ValidateRefreshTokenAsync(string token, CancellationToken ct = default)
		{
			var refreshToken = await _context.RefreshTokens
				.Include(rt => rt.User)
				.ThenInclude(u => u.UserRoles)
				.ThenInclude(ur => ur.Role)
				.FirstOrDefaultAsync(rt => rt.Token == token, ct);

			if (refreshToken == null) return null;
			if (refreshToken.IsRevoked) return null;
			if (refreshToken.ExpiryDate < DateTime.UtcNow) return null;

			return refreshToken.User;
		}

		public async Task RevokeRefreshTokenAsync(string token, string ipAddress, CancellationToken ct = default)
		{
			var refreshToken = await _context.RefreshTokens
				.FirstOrDefaultAsync(rt => rt.Token == token, ct);

			if (refreshToken != null)
			{
				refreshToken.IsRevoked = true;
				refreshToken.RevokedByIp = ipAddress;
				refreshToken.RevokedDate = DateTime.UtcNow;
				await _context.SaveChangesAsync(ct);
			}
		}

		public async Task RemoveExpiredRefreshTokensAsync(CancellationToken ct = default)
		{
			var expiredTokens = await _context.RefreshTokens
				.Where(rt => rt.ExpiryDate < DateTime.UtcNow || rt.IsRevoked)
				.ToListAsync(ct);

			_context.RefreshTokens.RemoveRange(expiredTokens);
			await _context.SaveChangesAsync(ct);
		}
	}
}
