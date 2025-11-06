using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using FinancialStatementService.Business.Abstract;

namespace FinancialStatementService.BackgroundServices
{
	/// <summary>
	/// Günde bir kere bilançoları otomatik olarak Fintables'tan çeken background service
	/// </summary>
	public class FinancialStatementUpdateWorker : BackgroundService
	{
		private readonly ILogger<FinancialStatementUpdateWorker> _logger;
		private readonly IServiceScopeFactory _scopeFactory;
		private readonly int _updateIntervalInHours;

		public FinancialStatementUpdateWorker(
			ILogger<FinancialStatementUpdateWorker> logger,
			IServiceScopeFactory scopeFactory,
			IConfiguration configuration)
		{
			_logger = logger;
			_scopeFactory = scopeFactory;
			// appsettings.json'dan güncelleme aralığını oku (varsayılan: 24 saat)
			_updateIntervalInHours = configuration.GetValue<int>("WorkerSettings:FinancialStatementUpdateIntervalInHours", 24);
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			_logger.LogInformation("Financial Statement Update Worker başlatıldı. Güncelleme aralığı: {UpdateInterval} saat.", _updateIntervalInHours);

			// İlk çalıştırmayı 1 dakika sonra yap (servis yeni başladığında hemen çalışmasın)
			await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

			while (!stoppingToken.IsCancellationRequested)
			{
				_logger.LogInformation("Bilanço verileri güncelleniyor (Web scraping başlatıldı)...");

				try
				{
					using (var scope = _scopeFactory.CreateScope())
					{
						var financialStatementManager = scope.ServiceProvider.GetRequiredService<IFinancialStatementManager>();

						// Fintables'tan bilançoları çek ve veritabanına kaydet
						var statements = await financialStatementManager.FetchAndSaveFinancialStatementsAsync();

						_logger.LogInformation("Bilanço verileri başarıyla güncellendi. Toplam {Count} bilanço çekildi.", statements.Count);
					}
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Bilanço verilerini güncellerken bir hata oluştu.");
					// Hata durumunda işlem devam eder, bir sonraki döngüde tekrar dener
				}

				// Belirlenen süre kadar bekle (varsayılan: 24 saat)
				_logger.LogInformation("Bir sonraki güncelleme {UpdateInterval} saat sonra yapılacak.", _updateIntervalInHours);
				await Task.Delay(TimeSpan.FromHours(_updateIntervalInHours), stoppingToken);
			}

			_logger.LogInformation("Financial Statement Update Worker sonlandırıldı.");
		}
	}
}

