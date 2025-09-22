using FinancialNewsService.Business.Abstract;
using FinancialNewsService.Entities;
using Microsoft.AspNetCore.Mvc;

namespace FinancialNewsService.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class FinancialNewsController : ControllerBase
	{
		private readonly IFinancialNewsManager _newsManager;

		public FinancialNewsController(IFinancialNewsManager newsManager)
		{
			_newsManager = newsManager;
		}

		[HttpGet]
		public async Task<ActionResult<List<FinancialNewsDto>>> GetAllNews()
		{
			try
			{
				var news = await _newsManager.GetAllNewsAsync();
				return Ok(news);
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { message = "Haberler getirilirken hata oluştu.", error = ex.Message });
			}
		}

		[HttpGet("{id}")]
		public async Task<ActionResult<FinancialNewsDto>> GetNewsById(int id)
		{
			try
			{
				var news = await _newsManager.GetNewsByIdAsync(id);
				if (news == null)
				{
					return NotFound(new { message = "Haber bulunamadı." });
				}
				return Ok(news);
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { message = "Haber getirilirken hata oluştu.", error = ex.Message });
			}
		}

		[HttpPost("fetch-and-save")]
		public async Task<ActionResult<object>> FetchAndSaveFinancialNews()
		{
			try
			{
				var news = await _newsManager.FetchAndSaveFinancialNewsAsync();
				return Ok(new { message = "Finans haberleri başarıyla çekildi ve kaydedildi.", count = news.Count });
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { message = "Haberler çekilirken hata oluştu.", error = ex.Message });
			}
		}

		[HttpDelete("all")]
		public async Task<ActionResult<object>> DeleteAllNews()
		{
			try
			{
				await _newsManager.DeleteAllNewsAsync();
				return Ok(new { message = "Tüm haberler başarıyla silindi." });
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { message = "Haberler silinirken hata oluştu.", error = ex.Message });
			}
		}
	}
}
