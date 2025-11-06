using Microsoft.EntityFrameworkCore;
using WatchlistService.DataAccess.Context;
using WatchlistService.Business.Abstract;
using WatchlistService.Business.Concrete;
using WatchlistService.DataAccess.Abstract;
using WatchlistService.DataAccess.Concrete;
using Shared.Extensions;
using Shared.Contracts;
using StackExchange.Redis;
using StockService.DataAccess.Redis;
using System.Text.Json.Serialization;
using Microsoft.OpenApi.Models;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);

// Merkezi loglama konfigürasyonu
builder.AddCentralizedLogging();

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

// CORS Konfigürasyonu
builder.Services.AddCors(options =>
{
	options.AddPolicy("CorsPolicy",
		policy => policy
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

// JWT Token Cache için Memory Cache ekle
builder.Services.AddMemoryCache();

builder.Services.AddControllers()
	.AddJsonOptions(o =>
	{
		o.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
	});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
	c.SwaggerDoc("v1", new OpenApiInfo { Title = "WatchlistService", Version = "v1" });
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

// Merkezi JWT Doğrulama
builder.Services.AddCentralizedJwt(builder.Configuration);

// Merkezi Authorization Policy'leri ekle
builder.Services.AddCentralizedAuthorization();

// DbContext
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContextPool<WatchlistDbContext>(options =>
	options.UseNpgsql(connectionString)
);

// Health Checks
builder.Services.AddHealthChecks();

// Repository ve Manager servisleri
builder.Services.AddScoped<IWatchlistRepository, WatchlistRepository>();
builder.Services.AddScoped<IWatchlistManager, WatchlistManager>();

// HttpClient'i servislere ekle - Timeout ve SSL ayarları ile
builder.Services.AddHttpClient<IWatchlistManager, WatchlistManager>(client =>
{
	client.Timeout = TimeSpan.FromSeconds(30);
	client.DefaultRequestHeaders.Add("User-Agent", "WatchlistService/1.0");
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

// Redis konfigürasyonu
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
	var config = builder.Configuration.GetValue<string>("Redis:Configuration");
	return ConnectionMultiplexer.Connect(config);
});

// RedisCacheService'i singleton olarak ekle
builder.Services.AddSingleton<IRedisCacheService, RedisCacheService>();

var app = builder.Build();

// Health Checks (zaten ekli)
// Response Compression (gzip/brotli)
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

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
app.UseCors("CorsPolicy");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

// Database migration
using (var scope = app.Services.CreateScope())
{
	var services = scope.ServiceProvider;
	var context = services.GetRequiredService<WatchlistDbContext>();
	context.Database.Migrate();
}

app.Run();
