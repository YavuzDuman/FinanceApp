using LLMService.Service;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Net.Http;
using LLMService.Entities;
using Microsoft.AspNetCore.Authorization;

[ApiController]
[Route("[controller]")]
[Authorize]
public class AIController : ControllerBase
{
	private readonly IHttpClientFactory _httpClientFactory;
	private readonly IConfiguration _configuration;

	public AIController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
	{
		_httpClientFactory = httpClientFactory;
		_configuration = configuration;
	}

	[HttpPost("ask")]
	public async Task<IActionResult> AskAI([FromBody] AskAIRequest request)
	{
		try
		{
			IAIService aiService;

			// Servis tipine göre AI servisi seç
			switch (request.ServiceType?.ToLower())
			{
				case "googlecloud":
					var projectId = _configuration["GoogleCloud:ProjectId"];
					var location = _configuration["GoogleCloud:Location"] ?? "us-central1";
					var modelName = _configuration["GoogleCloud:ModelName"] ?? "gemini-1.5-flash";
					var credentialsPath = _configuration["GoogleCloud:CredentialsPath"];

					if (string.IsNullOrEmpty(projectId))
						return BadRequest("Google Cloud Project ID bulunamadı.");

					aiService = new GoogleCloudAIService(projectId, location, modelName, credentialsPath);
					break;

				case "gemini":
				default:
					var httpClient = _httpClientFactory.CreateClient();
					var geminiKey = _configuration["GeminiAI:ApiKey"];
					
					if (string.IsNullOrEmpty(geminiKey))
						return BadRequest("Gemini API anahtarı bulunamadı.");

					aiService = new GeminiService(httpClient, geminiKey);
					break;
			}

			var response = await aiService.GetAIResponseAsync(request.Prompt);
			return Ok(new { response, serviceType = request.ServiceType ?? "gemini" });
		}
		catch (Exception ex)
		{
			return StatusCode(500, $"Bir hata oluştu: {ex.Message}");
		}
	}

	[HttpPost("ask-simple")]
	public async Task<IActionResult> AskAISimple([FromBody] string prompt)
	{
		try
		{
			var httpClient = _httpClientFactory.CreateClient();
			var geminiKey = _configuration["GeminiAI:ApiKey"];
			
			if (string.IsNullOrEmpty(geminiKey))
				return BadRequest("Gemini API anahtarı bulunamadı.");

			var aiService = new GeminiService(httpClient, geminiKey);
			var response = await aiService.GetAIResponseAsync(prompt);
			return Ok(response);
		}
		catch (Exception ex)
		{
			return StatusCode(500, $"Bir hata oluştu: {ex.Message}");
		}
	}
}

