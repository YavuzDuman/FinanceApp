//PortfolioService program.cs

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PortfolioService.Business.Abstract;
using PortfolioService.Business.Concrete;
using PortfolioService.DataAccess.Abstract;
using PortfolioService.DataAccess.Concrete;
using PortfolioService.DataAccess.Context;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers().AddJsonOptions(o =>
{
	o.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
	c.SwaggerDoc("v1", new OpenApiInfo { Title = "PortfolioService", Version = "v1" });
	var securityScheme = new OpenApiSecurityScheme
	{
		Name = "Authorization",
		Type = SecuritySchemeType.Http,
		Scheme = "bearer",
		BearerFormat = "JWT",
		In = ParameterLocation.Header,
		Description = "JWT Bearer token'ı 'Bearer {token}' formatında gönderin"
	};
	c.AddSecurityDefinition("Bearer", securityScheme);
	c.AddSecurityRequirement(new OpenApiSecurityRequirement
	{
		{
			securityScheme,
			Array.Empty<string>()
		}
	});
});



// DbContext'i servislere ekle ve veritabani baglanti dizesini al.
builder.Services.AddDbContext<PortfolioDatabaseContext>(options =>
	options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IPortfolioRepository, PortfolioRepository>();
builder.Services.AddScoped<IPortfolioManager, PortfolioManager>();

// HttpClient'i servislere ekle.
// PortfolioManager, StockService ile iletisim kurmak i�in buna ihtiya� duyar.
builder.Services.AddHttpClient();
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

// Debug logları temizlendi

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
	.AddJwtBearer(options =>
	{
		options.IncludeErrorDetails = false;
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
		// Varsayılan token ayrıştırma yeterli, ekstra event/log kaldırıldı
	});



// Authorization middleware'ini de ekle
builder.Services.AddAuthorization();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
	var services = scope.ServiceProvider;
	var context = services.GetRequiredService<PortfolioDatabaseContext>();
	context.Database.Migrate();
}

app.Run();