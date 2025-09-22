using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration; // IConfiguration için eklendi
using StockService.Business.Abstract;
using Microsoft.AspNetCore.SignalR;
using StockService.Hubs;

namespace StockService.BackgroundServices
{
	public class StockUpdateWorker : BackgroundService
	{
		private readonly ILogger<StockUpdateWorker> _logger;
		private readonly IServiceScopeFactory _scopeFactory;
		private readonly IHubContext<StockHub> _hubContext;
		private readonly int _updateIntervalInMinutes; // Yeni alan

		public StockUpdateWorker(
			ILogger<StockUpdateWorker> logger,
			IServiceScopeFactory scopeFactory,
			IHubContext<StockHub> hubContext,
			IConfiguration configuration) // IConfiguration eklendi
		{
			_logger = logger;
			_scopeFactory = scopeFactory;
			_hubContext = hubContext;
			// appsettings.json dosyasından güncelleme aralığını oku
			_updateIntervalInMinutes = configuration.GetValue<int>("WorkerSettings:StockUpdateIntervalInMinutes");
		}

		// StockService.BackgroundServices/StockUpdateWorker.cs
		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			_logger.LogInformation("Stock Update Worker başlatıldı. Güncelleme aralığı: {UpdateInterval} dakika.", _updateIntervalInMinutes);

			while (!stoppingToken.IsCancellationRequested)
			{
				_logger.LogInformation("Hisse senedi verileri güncelleniyor...");

				try
				{
					using (var scope = _scopeFactory.CreateScope())
					{
						var stockManager = scope.ServiceProvider.GetRequiredService<IStockManager>();

						// SADECE BU SATIR YETERLİ!
						// Bu metot hem veritabanını hem de Redis'i günceller.
						await stockManager.UpdateStocksFromExternalApiAsync();

						_logger.LogInformation("Hisse senedi verileri başarıyla veritabanında ve Redis cache'inde güncellendi.");

						// Güncelleme tamamlandığında SignalR üzerinden sinyal gönder
						await _hubContext.Clients.All.SendAsync("ReceiveStockUpdate", "Hisse senedi verileri güncellendi.");
					}
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Hisse senedi verilerini güncellerken bir hata oluştu.");
					// Hata yönetimi mantığı doğru, bir değişikliğe gerek yok.
				}

				await Task.Delay(TimeSpan.FromMinutes(_updateIntervalInMinutes), stoppingToken);
			}

			_logger.LogInformation("Stock Update Worker sonlandırıldı.");
		}
	}
}
