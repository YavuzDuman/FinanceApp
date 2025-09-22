using FinancialStatementService.Business.Abstract;
using Microsoft.AspNetCore.Mvc;

namespace FinancialStatementService.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class FinancialStatementController : ControllerBase
	{
		private readonly IFinancialStatementManager _financialStatementManager;

		public FinancialStatementController(IFinancialStatementManager financialStatementManager)
		{
			_financialStatementManager = financialStatementManager;
		}

		// Sembole ve tarihe göre bilanço verisini getirir
		[HttpGet("{symbol}/{date}")]
		public async Task<IActionResult> GetSymbolByNameAsync(string symbol)
		{
			var statement = await _financialStatementManager.GetSymbolByNameAsync(symbol);

			if (statement == null)
			{
				return NotFound($"Financial statement for symbol '{symbol}' was not found.");
			}

			return Ok(statement);
		}

		// Bilanço verilerini Fintables'tan çekip veritabanına kaydeder
		[HttpPost("fetch-and-save")]
		public async Task<IActionResult> FetchAndSaveFinancialStatements()
		{
			try
			{
				var statements = await _financialStatementManager.FetchAndSaveFinancialStatementsAsync();
				return Ok(new { Message = "Financial statements fetched and saved successfully.", Count = statements.Count });
			}
			catch (Exception ex)
			{
				// Hata oluşursa 500 Internal Server Error dön
				return StatusCode(500, $"An error occurred while fetching and saving financial statements: {ex.Message}");
			}
		}

		[HttpGet("all")]
		public async Task<IActionResult> GetAllSymbols()
		{
			var symbols = await _financialStatementManager.GetAllSymbolsAsync();
			return Ok(symbols);
		}

		[HttpGet("symbol/{symbol}")]
		public async Task<IActionResult> GetSymbolByName(string symbol)
		{
			var statement = await _financialStatementManager.GetSymbolByNameAsync(symbol);
			if (statement == null)
			{
				return NotFound($"Symbol '{symbol}' not found.");
			}
			return Ok(statement);
		}
	}
}