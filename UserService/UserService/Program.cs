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

// VERİTABANI BAĞLANTISI (DbContext Pooling)
builder.Services.AddDbContextPool<UserDatabaseContext>(options =>
	options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

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

// CACHE SERVİSİ - Token validation için
builder.Services.AddMemoryCache();


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

// CORS Konfigürasyonu - Frontend ile iletişim için
builder.Services.AddCors(options =>
{
	options.AddPolicy("CorsPolicy",
		builder => builder
			.WithOrigins(
				"http://localhost:5173",
				"https://localhost:5173",
				"https://localhost:5000",
				"http://localhost:5000"
			)
			.AllowAnyMethod()
			.AllowAnyHeader()
			.AllowCredentials());
});

builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
// Health Checks (service kayıtları Build'ten ÖNCE olmalı)
builder.Services.AddHealthChecks();
// Response Compression (gzip/brotli)
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

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
app.UseCors("CorsPolicy"); // CORS'u routing'den sonra, authentication'dan önce ekle
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

// Database migration ve default roller
try
{
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        var context = services.GetRequiredService<UserDatabaseContext>();
        context.Database.Migrate();
        
        // Varsayılan rolleri oluştur
        var roleManager = services.GetRequiredService<IRoleManager>();
        await roleManager.InitializeDefaultRolesAsync();
        Console.WriteLine("Varsayılan roller başarıyla oluşturuldu.");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Migration veya rol oluşturma hatası: {ex.Message}");
    Console.WriteLine($"Stack trace: {ex.StackTrace}");
}

app.Run();