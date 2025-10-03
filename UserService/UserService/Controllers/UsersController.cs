using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using UserService.Business.Abstract;
using UserService.Entities.Concrete;
using UserService.Entities.Dto;

namespace UserService.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	[Authorize] // Tüm endpoint'ler için authentication gerekli
	public class UsersController : ControllerBase
	{
		private readonly IUserManager _manager;
		private readonly IAuthorizationService _authService;
		private readonly IMapper _mapper;
		public UsersController(IUserManager manager, IAuthorizationService authorizationService, IMapper mapper)
		{
			_manager = manager;
			_authService = authorizationService;
			_mapper = mapper;
		}


		// GET /api/users
		// Tüm kullanıcıları getirir. Sadece Admin ve Manager görebilir.
		[HttpGet]
		[Authorize(Policy = "AdminOrManager")]
		public async Task<IActionResult> GetAll(CancellationToken ct)
			=> Ok(await _manager.GetAllUsersAsync(ct));

		// GET /api/users/orderbydate
		[HttpGet("orderbydate")]
		[Authorize(Policy = "AdminOrManager")]
		public async Task<IActionResult> GetAllOrderByDate(CancellationToken ct)
			=> Ok(await _manager.GetAllUsersOrderByDateAsync(ct));

		// GET /api/users/{id}
		[HttpGet("{id}")]
		public async Task<IActionResult> GetById(int id, CancellationToken ct)
		{
			var user = await _manager.GetUserByIdAsync(id, ct);
			if (user == null) return NotFound($"ID {id} ile eşleşen kullanıcı bulunamadı.");
			return Ok(user);
		}

		[HttpPost]
		[Authorize(Policy = "AdminOnly")]
		public async Task<IActionResult> Create([FromBody] User user, CancellationToken ct)
		{
			await _manager.CreateUserAsync(user, ct);
			return Ok("Kullanıcı eklendi.");
		}

		[HttpPut("{id}")]
		public async Task<IActionResult> Update(int id, [FromBody] UserDto updatedUserDto, CancellationToken ct)
		{
			var existingUser = await _manager.GetUserByIdAsync(id, ct);
			if (existingUser == null)
			{
				return NotFound($"ID {id} ile eşleşen kullanıcı bulunamadı.");
			}

			// Admin herkesi güncelleyebilir, diğerleri sadece kendilerini
			var authorizationResult = await _authService.AuthorizeAsync(User, existingUser, "OwnerOnly");
			if (!authorizationResult.Succeeded)
			{
				// Owner değilse Admin kontrolü yap
				var adminResult = await _authService.AuthorizeAsync(User, "AdminOnly");
				if (!adminResult.Succeeded)
				{
					return Forbid();
				}
			}

			await _manager.UpdateUserAsync(id, _mapper.Map<User>(updatedUserDto), ct);
			return Ok("Kullanıcı güncellendi.");
		}

		[HttpDelete("{id}")]
		public async Task<IActionResult> Delete(int id, CancellationToken ct)
		{
			var existingUser = await _manager.GetUserByIdAsync(id, ct);
			if (existingUser == null)
			{
				return NotFound($"ID {id} ile eşleşen kullanıcı bulunamadı.");
			}

			// Admin herkesi silebilir, diğerleri sadece kendilerini
			var authorizationResult = await _authService.AuthorizeAsync(User, existingUser, "OwnerOnly");
			if (!authorizationResult.Succeeded)
			{
				// Owner değilse Admin kontrolü yap
				var adminResult = await _authService.AuthorizeAsync(User, "AdminOnly");
				if (!adminResult.Succeeded)
				{
					return Forbid();
				}
			}

			await _manager.DeleteUserAsync(id, ct);
			return Ok("Kullanıcı silindi.");
		}

	}
}
