using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;
using Microsoft.Extensions.Configuration;
using Serilog.Sinks.MSSqlServer;
using System.Reflection; // Assembly ismini almak için eklendi

namespace Shared.Extensions
{
	public static class LoggingExtensions
	{
		/// <summary>
		/// Merkezi Serilog konfigürasyonunu ekler
		/// </summary>
		public static WebApplicationBuilder AddCentralizedLogging(this WebApplicationBuilder builder)
		{
            // Bağlantı dizesini IConfiguration üzerinden alıyoruz
            var connectionString = builder.Configuration.GetConnectionString("LogDatabaseConnection");

            // Loglama ortamını ve servisin adını belirle
            var serviceName = Assembly.GetEntryAssembly()?.GetName().Name ?? "UnknownService";

            // Taban logger konfigürasyonu (Console + File)
            var loggerConfig = new LoggerConfiguration()
				// Ortam bağımlı minimum seviyeler:
				.MinimumLevel.Information()
				.MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning) // Microsoft loglarını kısıtla
				.MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)

				// Enrichment (Zenginleştirme)
				.Enrich.FromLogContext()
				.Enrich.WithProperty("Service", serviceName) // Servisin adını loglara ekle (UserService, PortfolioService vb.)
				.Enrich.WithProperty("Environment", builder.Environment.EnvironmentName)
				.Enrich.WithMachineName() // Hata alınan metot (Serilog.Enrichers.Environment gerektirir)
				.Enrich.WithThreadId()    // Hata alınan metot (Serilog.Enrichers.Thread gerektirir)

				// 1. SINK: Console (Geliştirme için standart log)
                .WriteTo.Console()

				// 2. SINK: File (TXT ve JSON dosyaları, mevcut yapınızdan alındı)
                .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day)

				// Sizin eski projenizden gelen filtreyi, sadece talep (request) ve hata loglarını dosyaya yazmak için kullanıyoruz
                .Filter.ByIncludingOnly(logEvent =>
				{
					var isRequestLog = logEvent.Properties.ContainsKey("IsRequestLog");
					var isError = logEvent.Level == Serilog.Events.LogEventLevel.Error;
					return isRequestLog || isError;
                });

            // 3. SINK: MSSQL SERVER (Veritabanı Kaydı) — bağlantı dizesi mevcutsa ekle
            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                try
                {
                    loggerConfig = loggerConfig.WriteTo.MSSqlServer(
                        connectionString: connectionString,
                        sinkOptions: new MSSqlServerSinkOptions
                        {
                            TableName = "Logs",
                            // Başlangıçta bağlantı sorunlarında çöküşü önlemek için auto-create kapalı
                            AutoCreateSqlTable = false
                        },
                        restrictedToMinimumLevel: LogEventLevel.Warning // Sadece Uyarı ve üstü (Error, Fatal) logları DB'ye yaz
                    );
                }
                catch (Exception ex)
                {
                    // Sink eklenemese de uygulama çalışmaya devam etmeli
                    Console.WriteLine($"UYARI: MSSQL sink eklenemedi: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine("UYARI: LogDatabaseConnection bulunamadı. MSSQL sink devre dışı, Console/File kullanılacak.");
            }

            Log.Logger = loggerConfig.CreateLogger();

			builder.Host.UseSerilog();

			return builder;
		}
	}
}
