namespace LLMService.Service
{
	public interface IAIService
	{
		Task<string> GetAIResponseAsync(string prompt);
	}
}
