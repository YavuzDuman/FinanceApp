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
		private readonly int _updateIntervalInMinutes; 

		public StockUpdateWorker(
			ILogger<StockUpdateWorker> logger,
			IServiceScopeFactory scopeFactory,
			IHubContext<StockHub> hubContext,
			IConfiguration configuration) 
		{
			_logger = logger;
			_scopeFactory = scopeFactory;
			_hubContext = hubContext;
			// appsettings.json dosyasından güncelleme aralığını oku (varsayılan: 30 dakika)
			_updateIntervalInMinutes = configuration.GetValue<int>("WorkerSettings:StockUpdateIntervalInMinutes", 30);
			
			// Minimum 15 dakika kontrolü (çok sık güncelleme CPU'yu yorar)
			if (_updateIntervalInMinutes < 15)
			{
				_logger.LogWarning("StockUpdateIntervalInMinutes 15 dakikadan az olamaz. 30 dakika kullanılıyor.");
				_updateIntervalInMinutes = 30;
			}
		}

		// StockService.BackgroundServices/StockUpdateWorker.cs
		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			_logger.LogInformation("Stock Update Worker başlatıldı. Güncelleme aralığı: {UpdateInterval} dakika.", _updateIntervalInMinutes);

			// İlk çalıştırmayı 2 dakika sonra yap (servis başlarken CPU'yu yormasın)
			await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);

			while (!stoppingToken.IsCancellationRequested)
			{
				var startTime = DateTime.UtcNow;
				_logger.LogInformation("Hisse senedi verileri güncelleniyor...");

				try
				{
					using (var scope = _scopeFactory.CreateScope())
					{
						var stockManager = scope.ServiceProvider.GetRequiredService<IStockManager>();

						// Bu metot hem veritabanını hem de Redis'i günceller.
						await stockManager.UpdateStocksFromExternalApiAsync();

						var duration = (DateTime.UtcNow - startTime).TotalSeconds;
						_logger.LogInformation("Hisse senedi verileri başarıyla güncellendi. Süre: {Duration} saniye", duration);

						// Güncelleme tamamlandığında SignalR üzerinden sinyal gönder
						try
						{
							await _hubContext.Clients.All.SendAsync("ReceiveStockUpdate", "Hisse senedi verileri güncellendi.", stoppingToken);
						}
						catch (Exception hubEx)
						{
							// SignalR hatası kritik değil, logla ve devam et
							_logger.LogWarning(hubEx, "SignalR bildirimi gönderilemedi.");
						}
					}
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Hisse senedi verilerini güncellerken bir hata oluştu.");
					// Hata durumunda bir sonraki güncellemeye kadar bekle
				}

				// Belirlenen süre kadar bekle
				_logger.LogDebug("Bir sonraki güncelleme {UpdateInterval} dakika sonra yapılacak.", _updateIntervalInMinutes);
				await Task.Delay(TimeSpan.FromMinutes(_updateIntervalInMinutes), stoppingToken);
			}

			_logger.LogInformation("Stock Update Worker sonlandırıldı.");
		}
	}
}
