using AutoMapper;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace WebApi.Helpers.Jwt
{
	public class JwtTokenGenerator
	{
		private readonly IConfiguration _config;
		private readonly IMapper _mapper;

		public JwtTokenGenerator(IConfiguration config, IMapper mapper)
		{
			_config = config;
			_mapper = mapper;
		}

		public string GenerateToken(UserService.Entities.Concrete.User user)
		{
			var claims = new List<Claim>
			{
				new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
				new Claim(ClaimTypes.Name, user.Username),
				new Claim(ClaimTypes.Email, user.Email),
			};

			if (user.UserRoles != null)
			{
				foreach (var role in user.UserRoles)
				{
					claims.Add(new Claim(ClaimTypes.Role, role.Role.Name));
				}
			}



			var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
			var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

			var token = new JwtSecurityToken(
				issuer: _config["Jwt:Issuer"],
				audience: _config["Jwt:Audience"],
				claims: claims,
				expires: DateTime.Now.AddMinutes(Convert.ToDouble(_config["Jwt:ExpireMinutes"])),
				signingCredentials: creds
			);

			return new JwtSecurityTokenHandler().WriteToken(token);
		}

		public string GenerateRefreshToken()
		{
			var randomNumber = new byte[256];
			using var rng = RandomNumberGenerator.Create();
			rng.GetBytes(randomNumber);
			return Convert.ToBase64String(randomNumber);
		}



	}
}
