using Microsoft.AspNetCore.Mvc;
using StockService.Business.Abstract;
using StockService.Entities.Concrete;
using Microsoft.AspNetCore.Authorization;

namespace StockService.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	[Authorize]
	public class StocksController : ControllerBase
	{
		private readonly IStockManager _stockManager;

		public StocksController(IStockManager stockManager)
		{
			_stockManager = stockManager;
		}

	[HttpGet]
	public async Task<IActionResult> GetAllStocks()
	{
		try
		{
			var stocks = await _stockManager.GetAllStocksAsync();
			if (stocks == null || !stocks.Any())
			{
				Console.WriteLine("UYARI: GetAllStocksAsync null veya boş döndü.");
				return Ok(new List<Stock>()); // Boş liste döndür
			}
			Console.WriteLine($"GetAllStocks: {stocks.Count} hisse bulundu.");
			return Ok(stocks);
		}
		catch (Exception ex)
		{
			Console.WriteLine($"GetAllStocks hatası: {ex.Message}");
			Console.WriteLine($"Stack trace: {ex.StackTrace}");
			if (ex.InnerException != null)
			{
				Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
			}
			return StatusCode(500, new { message = "Hisse senetleri yüklenirken bir hata oluştu.", error = ex.Message });
		}
	}

		[HttpGet("{symbol}")]
		public async Task<IActionResult> GetStockBySymbol(string symbol)
		{
			var stock = await _stockManager.GetStockBySymbolAsync(symbol);
			if (stock == null)
			{
				return NotFound($"Stock with symbol {symbol} not found.");
			}
			return Ok(stock);
		}

	[HttpPost("update-from-api")]
	[Authorize(Policy = "AdminOnly")] // Sadece Admin bu işlemi yapabilir
	public async Task<IActionResult> UpdateStocksFromApi()
	{
		await _stockManager.UpdateStocksFromExternalApiAsync();
		return Ok("Stocks updated successfully from external API.");
	}

	[HttpGet("{symbol}/historical")]
	public async Task<IActionResult> GetHistoricalData(
		string symbol, 
		[FromQuery] string period = "1mo", 
		[FromQuery] string interval = "1d")
	{
		Console.WriteLine($"[CONTROLLER] GetHistoricalData çağrıldı - Symbol: {symbol}, Period: {period}, Interval: {interval}");
		try
		{
			var historicalData = await _stockManager.GetHistoricalDataAsync(symbol, period, interval);
			Console.WriteLine($"[CONTROLLER] Manager'dan dönen veri sayısı: {historicalData?.Count ?? 0}");
			
			if (historicalData == null || !historicalData.Any())
			{
				return NotFound(new { message = $"{symbol} için tarihsel veri bulunamadı." });
			}

			return Ok(historicalData);
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Tarihsel veri hatası: {ex.Message}");
			return BadRequest(new { message = "Tarihsel veri çekilirken hata oluştu." });
		}
	}
	}
}