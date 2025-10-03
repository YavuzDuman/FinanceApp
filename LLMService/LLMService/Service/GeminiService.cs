using LLMService.Service;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public class GeminiService : IAIService
{
	private readonly HttpClient _httpClient;
	private readonly string _apiKey;

	private const string ModelName = "gemini-1.5-flash"; // Daha hızlı ve daha az kota kullanan model

	public GeminiService(HttpClient httpClient, string apiKey)
	{
		_httpClient = httpClient;
		_apiKey = apiKey;
	}

	public async Task<string> GetAIResponseAsync(string prompt)
	{
		var requestBody = new
		{
			contents = new[]
			{
				new
				{
					parts = new[]
					{
						new { text = prompt }
					}
				}
			}
		};

		var jsonContent = new StringContent(
			JsonSerializer.Serialize(requestBody),
			Encoding.UTF8,
			"application/json"
		);

		// URL'yi güncelleyip ModelName değişkenini kullanıyoruz
		var url = $"https://generativelanguage.googleapis.com/v1beta/models/{ModelName}:generateContent?key={_apiKey}";
		var response = await _httpClient.PostAsync(url, jsonContent);

		if (response.IsSuccessStatusCode)
		{
			var jsonResponse = await response.Content.ReadAsStringAsync();
			using var doc = JsonDocument.Parse(jsonResponse);
			var text = doc.RootElement
				.GetProperty("candidates")[0]
				.GetProperty("content")
				.GetProperty("parts")[0]
				.GetProperty("text")
				.GetString();

			return text;
		}
		else
		{
			var errorContent = await response.Content.ReadAsStringAsync();
			throw new Exception($"API call failed with status code: {response.StatusCode} and message: {errorContent}");
		}
	}
}