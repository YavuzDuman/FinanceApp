//UserService program.cs

using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using UserService.Business.Abstract;
using UserService.Business.Concrete;
using UserService.DataAccess.Abstract;
using UserService.DataAccess.Concrete;
using UserService.DataAccess.Context;
using UserService.Entities.Concrete;
using UserService.Helpers.Hashing;
using System.IdentityModel.Tokens.Jwt;
using Shared.Extensions;
using WebApi.Helpers.Jwt;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Merkezi loglama konfigürasyonu
builder.AddCentralizedLogging();

// VER�TABANI BA�LANTISI
builder.Services.AddDbContext<UserDatabaseContext>(options =>
	options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// HASHING SERV�S�
builder.Services.AddScoped<PasswordHasher>();

// REPOSITORYLER
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IUserRoleRepository, UserRoleRepository>();
builder.Services.AddScoped<IRepository<UserRole>, EfRepository<UserRole>>();
builder.Services.AddScoped<IRepository<Role>, EfRepository<Role>>();

// MANAGER'LAR VE DİĞER SERVİSLER
builder.Services.AddScoped<IAuthManager, AuthManager>();
builder.Services.AddScoped<IUserManager, UserManager>();
builder.Services.AddScoped<IRoleManager, RoleManager>();
builder.Services.AddScoped<JwtTokenGenerator>();


JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

// JWT Konfig�rasyonu
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
	.AddJwtBearer(options =>
	{
		options.IncludeErrorDetails = true; // Hata detaylar�n� konsola yazd�r�r.
		options.TokenValidationParameters = new TokenValidationParameters
		{
			ValidateIssuerSigningKey = true,
			IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])),
			ValidateIssuer = true,
			ValidIssuer = builder.Configuration["Jwt:Issuer"],
			ValidateAudience = true,
			ValidAudience = builder.Configuration["Jwt:Audience"],
			ValidateLifetime = true
		};
	});

// Merkezi Authorization Policy'leri ekle
builder.Services.AddCentralizedAuthorization();

builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
builder.Services.AddControllers()
	.AddFluentValidation(fv =>
	{
		// Validat�rleri otomatik olarak bul ve kaydet
		fv.RegisterValidatorsFromAssemblyContaining<Program>();

		fv.DisableDataAnnotationsValidation = false;
	});

// Swagger/OpenAPI ayarlar�
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Merkezi middleware'leri ekle
app.UseCentralizedMiddleware();

// Routing, Authentication ve Authorization middleware'lerini do�ru s�rada ekle.
// Bu s�ralama, API isteklerinin do�ru �ekilde i�lenmesi i�in hayati �nem ta��r.
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Varsayılan rolleri oluştur
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<IRoleManager>();
    await roleManager.InitializeDefaultRolesAsync();
}

app.Run();