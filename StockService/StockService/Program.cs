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
// Redis bağlantısı ZORUNLU - Veriler Redis'e yazılacak
// Environment variable formatı: Redis__Configuration (çift alt çizgi)
var redisConfig = builder.Configuration.GetValue<string>("Redis:Configuration") 
                  ?? builder.Configuration.GetValue<string>("Redis__Configuration");

// Debug: Configuration değerini logla (şifreyi gizle)
if (!string.IsNullOrWhiteSpace(redisConfig))
{
    var maskedConfig = redisConfig.Length > 30 
        ? redisConfig.Substring(0, 30) + "..." 
        : redisConfig;
    Console.WriteLine($"Redis config bulundu (ilk 30 karakter): {maskedConfig}");
}
else
{
    Console.WriteLine("UYARI: Redis:Configuration ve Redis__Configuration ikisi de boş!");
}

if (string.IsNullOrWhiteSpace(redisConfig))
{
    Console.WriteLine("═══════════════════════════════════════════════════════");
    Console.WriteLine("HATA: Redis yapılandırması bulunamadı!");
    Console.WriteLine("Redis bağlantısı için 'Redis:Configuration' veya 'Redis__Configuration' ayarını yapılandırın.");
    Console.WriteLine("Render.com'da environment variable: Redis__Configuration");
    Console.WriteLine("Örnek format: rediss://default:password@host:port");
    Console.WriteLine("═══════════════════════════════════════════════════════");
    throw new InvalidOperationException("Redis configuration is required. Please set 'Redis__Configuration' environment variable in Render.com");
}

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    try
    {
        ConfigurationOptions options;
        
        // Upstash rediss:// veya redis:// URL'si verilmişse parse et
        if (redisConfig.StartsWith("rediss://", StringComparison.OrdinalIgnoreCase) || 
            redisConfig.StartsWith("redis://", StringComparison.OrdinalIgnoreCase))
        {
            // URI parsing - port bilgisi eksik olabilir, manuel parse et
            string host;
            int port;
            string? password = null;
            bool useSsl = redisConfig.StartsWith("rediss://", StringComparison.OrdinalIgnoreCase);
            
            try
            {
                // URI'yi parse et - port bilgisi olmayabilir (Upstash için)
                var uriString = redisConfig;
                
                // Önce normal URI parsing dene
                Uri? uri = null;
                try
                {
                    uri = new Uri(uriString);
                    // Eğer port -1 ise (port belirtilmemiş), manuel parse et
                    if (uri.Port == -1)
                    {
                        uri = null; // Manuel parse'e geç
                    }
                }
                catch
                {
                    // URI parse edilemedi, manuel parse'e geç
                    uri = null;
                }
                
                if (uri != null)
                {
                    // Normal URI parsing başarılı
                    host = uri.Host;
                    port = uri.Port;
                    
                    // Port kontrolü
                    if (port <= 0 || port > 65535)
                    {
                        port = useSsl ? 6380 : 6379;
                        Console.WriteLine($"UYARI: Redis URI'de geçerli port bulunamadı. Varsayılan port kullanılıyor: {port}");
                    }
                    
                    // UserInfo'dan password'ü çıkar
                    if (!string.IsNullOrEmpty(uri.UserInfo))
                    {
                        var userInfoParts = uri.UserInfo.Split(':');
                        if (userInfoParts.Length >= 2)
                        {
                            password = string.Join(":", userInfoParts.Skip(1));
                        }
                        else if (userInfoParts.Length == 1 && userInfoParts[0].StartsWith(":"))
                        {
                            password = userInfoParts[0].Substring(1);
                        }
                        else
                        {
                            password = userInfoParts[0];
                        }
                    }
                }
                else
                {
                    // Manuel parse - port bilgisi yoksa (Upstash formatı: rediss://default:password@host)
                    var parts = uriString.Split('@');
                    if (parts.Length == 2)
                    {
                        var userPass = parts[0].Replace("rediss://", "").Replace("redis://", "");
                        var hostPart = parts[1];
                        
                        var userPassParts = userPass.Split(':');
                        if (userPassParts.Length >= 2)
                        {
                            password = string.Join(":", userPassParts.Skip(1));
                        }
                        
                        // Host'ta port var mı kontrol et
                        var hostParts = hostPart.Split(':');
                        if (hostParts.Length == 2 && int.TryParse(hostParts[1], out var parsedPort))
                        {
                            host = hostParts[0];
                            port = parsedPort;
                        }
                        else
                        {
                            host = hostPart;
                            port = useSsl ? 6380 : 6379; // Varsayılan port
                        }
                    }
                    else
                    {
                        throw new ArgumentException("Redis URI formatı geçersiz. Örnek: rediss://default:password@host:port veya rediss://default:password@host");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"═══════════════════════════════════════════════════════");
                Console.WriteLine($"HATA: Redis URI parse edilemedi!");
                Console.WriteLine($"URI (ilk 50 karakter): {redisConfig.Substring(0, Math.Min(50, redisConfig.Length))}...");
                Console.WriteLine($"Hata: {ex.Message}");
                Console.WriteLine($"═══════════════════════════════════════════════════════");
                throw new ArgumentException($"Geçersiz Redis URI formatı. Örnek: rediss://default:password@host:port veya rediss://default:password@host", ex);
            }

            options = new ConfigurationOptions
            {
                AbortOnConnectFail = false,
                Ssl = useSsl,
                ConnectRetry = 5,
                ConnectTimeout = 10000,
                SyncTimeout = 5000,
                AsyncTimeout = 5000
            };

            options.EndPoints.Add(host, port);
            
            if (!string.IsNullOrEmpty(password))
            {
                options.Password = password;
            }

            Console.WriteLine($"Redis bağlantısı kuruluyor: {host}:{port} (SSL: {useSsl})");
            
            var connection = ConnectionMultiplexer.Connect(options);
            
            // Bağlantı durumunu kontrol et
            if (connection.IsConnected)
            {
                Console.WriteLine("✓ Redis bağlantısı başarıyla kuruldu!");
            }
            else
            {
                Console.WriteLine("⚠ UYARI: Redis bağlantısı kuruldu ancak henüz bağlı değil. Bağlantı kurulmaya çalışılıyor...");
            }
            
            return connection;
        }
        else
        {
            // Anahtar=değer formatı veya connection string formatı için
            // Ama önce boş olmadığından emin ol
            if (string.IsNullOrWhiteSpace(redisConfig))
            {
                throw new ArgumentException("Redis configuration string is empty. Please provide a valid Redis connection string.");
            }
            
            Console.WriteLine("Redis connection string formatı kullanılıyor (rediss:// değil)");
            
            // abortConnect=false ekle (yoksa)
            var configWithAbort = redisConfig;
            if (!configWithAbort.Contains("abortConnect", StringComparison.OrdinalIgnoreCase))
            {
                configWithAbort += ",abortConnect=false";
            }
            
            var connection = ConnectionMultiplexer.Connect(configWithAbort);
            
            if (connection.IsConnected)
            {
                Console.WriteLine("✓ Redis bağlantısı başarıyla kuruldu!");
            }
            
            return connection;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"═══════════════════════════════════════════════════════");
        Console.WriteLine($"HATA: Redis bağlantısı kurulamadı!");
        Console.WriteLine($"Hata mesajı: {ex.Message}");
        Console.WriteLine($"Hata tipi: {ex.GetType().Name}");
        if (ex.InnerException != null)
        {
            Console.WriteLine($"İç hata: {ex.InnerException.Message}");
        }
        Console.WriteLine($"Redis Config (ilk 50 karakter): {redisConfig?.Substring(0, Math.Min(50, redisConfig?.Length ?? 0))}...");
        Console.WriteLine($"═══════════════════════════════════════════════════════");
        throw; // Redis zorunlu olduğu için hatayı fırlat
    }
});

// RedisCacheService'i de singleton olarak ekle
builder.Services.AddSingleton<IRedisCacheService, RedisCacheService>();
Console.WriteLine("Redis servisi yapılandırıldı.");

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
