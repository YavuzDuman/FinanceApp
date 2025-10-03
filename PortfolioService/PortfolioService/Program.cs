//PortfolioService program.cs

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using MassTransit;
using Shared.Contracts;
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
using Shared.Extensions;
using FluentValidation.AspNetCore;
using Serilog;
using StackExchange.Redis;
using StockService.DataAccess.Redis;

var builder = WebApplication.CreateBuilder(args);

// Merkezi loglama konfigürasyonu
builder.AddCentralizedLogging();
// MassTransit & RabbitMQ
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<StockPriceUpdatedConsumer>();
	x.SetKebabCaseEndpointNameFormatter();
    x.UsingRabbitMq((context, cfg) =>
    {
        var uri = builder.Configuration.GetValue<string>("RabbitMQ:Uri");
        cfg.Host(new Uri(uri));
        cfg.ConfigureEndpoints(context);
    });
});

// Add services to the container.

// JWT Token Cache için Memory Cache ekle
builder.Services.AddMemoryCache();

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

// Merkezi JWT Doğrulama - Güvenlik için geri eklendi
// Çift katmanlı koruma: API Gateway + Service Level
builder.Services.AddCentralizedJwt(builder.Configuration);

// Merkezi Authorization Policy'leri ekle
builder.Services.AddCentralizedAuthorization();

// DbContext'i servislere ekle ve veritabani baglanti dizesini al.
builder.Services.AddDbContext<PortfolioDatabaseContext>(options =>
	options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IPortfolioRepository, PortfolioRepository>();
builder.Services.AddScoped<IPortfolioManager, PortfolioManager>();


builder.Services.AddControllers()
	.AddFluentValidation(fv =>
	{
		// Validat�rleri otomatik olarak bul ve kaydet
		fv.RegisterValidatorsFromAssemblyContaining<Program>();

		fv.DisableDataAnnotationsValidation = false;
	});

// HttpClient'i servislere ekle - Timeout ve SSL ayarları ile
builder.Services.AddHttpClient<IPortfolioManager, PortfolioManager>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("User-Agent", "PortfolioService/1.0");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
})
.ConfigurePrimaryHttpMessageHandler(() => 
{
    var handler = new HttpClientHandler();
    
    // Development ortamında SSL sertifika doğrulamasını devre dışı bırak
    if (builder.Environment.IsDevelopment())
    {
        handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
    }
    
    return handler;
});

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
	var config = builder.Configuration.GetValue<string>("Redis:Configuration");
	return ConnectionMultiplexer.Connect(config);
});

// RedisCacheService'i de singleton olarak ekle
builder.Services.AddSingleton<IRedisCacheService, RedisCacheService>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Merkezi middleware'leri ekle
app.UseCentralizedMiddleware();

app.UseRouting();
// JWT doğrulama - Güvenlik için geri eklendi
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