using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using UserService.Business.Abstract;
using UserService.Entities.Dto;
using WebApi.Helpers.Jwt;

namespace UserService.Controller
{
	[ApiController]
	[Route("api/[controller]")]
	public class AuthController : ControllerBase
	{
		private readonly IAuthManager _authManager;
		private readonly JwtTokenGenerator _jwtTokenGenerator;


		public AuthController(IAuthManager authManager, JwtTokenGenerator jwtTokenGenerator)
		{
			_authManager = authManager;
			_jwtTokenGenerator = jwtTokenGenerator;
		}

		[HttpPost("register")]
		public async Task<IActionResult> Register(RegisterDto registerUser, CancellationToken ct)
		{
			var ok = await _authManager.RegisterUserAsync(registerUser, ct);
			if (!ok) return BadRequest("Bu kullanıcı adı veya e-posta zaten kayıtlı.");

			return Ok("Kullanıcı başarıyla kaydedildi.");
		}

		[HttpPost("login")]
		public async Task<IActionResult> Login([FromBody] LoginDto loginUser, CancellationToken ct)
		{
			var user = await _authManager.LoginUserAsync(loginUser, ct);
			if (user == null)
			{
				return Unauthorized("Kullanıcı adı veya şifre yanlış.");
			}
			var accessToken = _jwtTokenGenerator.GenerateToken(user);

			Console.WriteLine($"Generated Token: {accessToken}");

			return Ok(new { accessToken, user.UserId, user.Username, user.Email });
		}


	}
}
