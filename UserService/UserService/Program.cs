//UserService program.cs

using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
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
using WebApi.Helpers.Authorization;
using WebApi.Helpers.Jwt;
using System.IdentityModel.Tokens.Jwt;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// VERÝTABANI BAÐLANTISI
builder.Services.AddDbContext<UserDatabaseContext>(options =>
	options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// HASHING SERVÝSÝ
builder.Services.AddScoped<PasswordHasher>();

// REPOSITORYLER
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<IRepository<UserRole>, EfRepository<UserRole>>();
builder.Services.AddScoped<IRepository<Role>, EfRepository<Role>>();

// MANAGER'LAR VE DÝÐER SERVÝSLER
builder.Services.AddScoped<IAuthManager, AuthManager>();
builder.Services.AddScoped<IUserManager, UserManager>();
builder.Services.AddScoped<JwtTokenGenerator>();

// JWT Claim eþleþtirmesini temizle.
// Bu, token'ýn içindeki claim'lerin, uzun þema adlarýyla kalmasýný saðlar.
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

// JWT Konfigürasyonu
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
	.AddJwtBearer(options =>
	{
		options.IncludeErrorDetails = true; // Hata detaylarýný konsola yazdýrýr.
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

// Authorization Handler'ý Dependency Injection'a kaydet.
builder.Services.AddSingleton<IAuthorizationHandler, OwnerAuthorizationHandler>();

builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
builder.Services.AddControllers()
	.AddFluentValidation(fv =>
	{
		// Validatörleri otomatik olarak bul ve kaydet
		fv.RegisterValidatorsFromAssemblyContaining<Program>();

		fv.DisableDataAnnotationsValidation = false;
	});

// Swagger/OpenAPI ayarlarý
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Authorization servisini ekle
builder.Services.AddAuthorization();


var app = builder.Build();

// HTTP Ýstek Ýþlem Hattýný Yapýlandýr.
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Routing, Authentication ve Authorization middleware'lerini doðru sýrada ekle.
// Bu sýralama, API isteklerinin doðru þekilde iþlenmesi için hayati önem taþýr.
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();