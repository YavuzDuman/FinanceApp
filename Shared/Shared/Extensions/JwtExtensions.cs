using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace Shared.Extensions
{
	public static class JwtExtensions
	{
		public static IServiceCollection AddCentralizedJwt(this IServiceCollection services, IConfiguration configuration)
		{
			JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
			services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
				.AddJwtBearer(options =>
				{
					options.IncludeErrorDetails = true;
					options.TokenValidationParameters = new TokenValidationParameters
					{
						ValidateIssuerSigningKey = true,
						IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!)),
						ValidateIssuer = true,
						ValidIssuer = configuration["Jwt:Issuer"],
						ValidateAudience = true,
						ValidAudience = configuration["Jwt:Audience"],
						ValidateLifetime = true,
						ClockSkew = TimeSpan.Zero
					};
				});

			services.AddAuthorization();
			return services;
		}
	}
}


