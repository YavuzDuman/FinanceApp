using AutoMapper;
using UserService.Business.Abstract;
using UserService.DataAccess.Abstract;
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

		public AuthManager(IAuthRepository repo, PasswordHasher passwordHasher, IMapper mapper) // Bağımlılıklar eklendi
		{
			_repo = repo;
			_passwordHasher = passwordHasher;
			_mapper = mapper;
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
			
			await _repo.RegisterUserAsync(userToRegister, ct);
			return true;
		}

		public Task<User?> GetUserByIdAsync(int userId, CancellationToken ct = default)
			=> _repo.GetUserByIdAsync(userId, ct);

	}
}
