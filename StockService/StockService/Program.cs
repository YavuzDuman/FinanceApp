using Microsoft.EntityFrameworkCore;
using StockService.Business.Abstract;
using StockService.Business.Concrete;
using StockService.DataAccess.Abstract;
using StockService.DataAccess.Concrete;
using StockService.DataAccess.Context;
using StockService.DataAccess.Redis;
using StackExchange.Redis; 
using StockService.BackgroundServices; 
using MassTransit;
using StockService.Helpers;
using Shared.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddCentralizedLogging();

// RabbitMQ opsiyonel: URI verilmemişse MassTransit'i ekleme
var rabbitUri = builder.Configuration.GetValue<string>("RabbitMQ:Uri");
if (!string.IsNullOrWhiteSpace(rabbitUri))
{
    builder.Services.AddMassTransit(x =>
    {
        x.SetKebabCaseEndpointNameFormatter();
        x.UsingRabbitMq((context, cfg) =>
        {
            cfg.Host(new Uri(rabbitUri));
            cfg.ConfigureEndpoints(context);
        });
    });
}

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

// Add services to the container.
builder.Services.AddControllers()
	.AddJsonOptions(options =>
	{
		options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
		options.JsonSerializerOptions.WriteIndented = false;
		// DateTime'ları ISO 8601 formatında serialize et (default olarak zaten yapıyor)
		options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
	});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Merkezi JWT Doğrulama
builder.Services.AddCentralizedJwt(builder.Configuration);

// Merkezi Authorization Policy'leri ekle
builder.Services.AddCentralizedAuthorization();

// Health Checks
builder.Services.AddHealthChecks();

// Response Compression (gzip/brotli)
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

// DbContext
builder.Services.AddDbContextPool<StockDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// DI Container'a servisleri ekle
builder.Services.AddHttpClient();

// Redis ba�lant�s�n� singleton olarak ekle
// Bu, uygulaman�n ya�am d�ng�s� boyunca tek bir Redis ba�lant�s�n�n kullan�lmas�n� sa�lar.
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var config = builder.Configuration.GetValue<string>("Redis:Configuration");
    if (string.IsNullOrWhiteSpace(config))
        throw new ArgumentException("Redis configuration is missing", nameof(config));

    try
    {
        // Upstash rediss:// URL'si verilmişse güvenli şekilde parse et ve TLS + retry ayarlarıyla bağlan
        if (config.StartsWith("rediss://", StringComparison.OrdinalIgnoreCase))
        {
            var uri = new Uri(config);
            var password = uri.UserInfo?.StartsWith(":") == true ? uri.UserInfo.Substring(1) : uri.UserInfo;

            var options = new ConfigurationOptions
            {
                AbortOnConnectFail = false,
                Ssl = true,
                ConnectRetry = 3,
                ConnectTimeout = 5000
            };
            options.EndPoints.Add(uri.Host, uri.Port == -1 ? 6379 : uri.Port);
            if (!string.IsNullOrEmpty(password)) options.Password = password;

            return ConnectionMultiplexer.Connect(options);
        }

        // Anahtar=değer formatı için doğrudan bağlan (abortConnect=false eklemen önerilir)
        return ConnectionMultiplexer.Connect(config);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Redis connect failed: {ex.Message}");
        // Bağlantı kurulamasa bile uygulamanın ayağa kalkması için abortConnect=false ile tekrar denemeye devam et
        return ConnectionMultiplexer.Connect(config + (config.Contains("abortConnect", StringComparison.OrdinalIgnoreCase) ? string.Empty : ",abortConnect=false"));
    }
});

// RedisCacheService'i de singleton olarak ekle
builder.Services.AddSingleton<IRedisCacheService, RedisCacheService>();

// SignalR servisini ekle
builder.Services.AddSignalR();

// Di�er servislerin kayd�
builder.Services.AddScoped<IExternalApiService, ExternalApiService>();
builder.Services.AddScoped<IStockRepository, StockRepository>();
builder.Services.AddScoped<IStockManager, StockManager>();

// BackgroundService'i ekle
builder.Services.AddHostedService<StockUpdateWorker>();

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

// Routing'i ve Authorization'� etkinle�tir
app.UseRouting();
app.UseCors("CorsPolicy"); // CORS'u routing'den sonra, authentication'dan önce ekle
app.UseAuthentication();
app.UseAuthorization();

// SignalR Hub'� haritaland�r
app.UseEndpoints(endpoints =>
{
	endpoints.MapControllers();
	endpoints.MapHub<StockService.Hubs.StockHub>("/stockHub");
    endpoints.MapHealthChecks("/health");
});

// Veritaban� migrate i�lemi
try
{
	using (var scope = app.Services.CreateScope())
	{
		var db = scope.ServiceProvider.GetRequiredService<StockDbContext>();
		db.Database.Migrate();
	}
}
catch (Exception ex)
{
	Console.WriteLine("Migration error: " + ex.Message);
}

app.Run();
