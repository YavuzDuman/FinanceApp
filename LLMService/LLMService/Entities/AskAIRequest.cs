namespace LLMService.Entities
{
	public class AskAIRequest
	{
		public string Prompt { get; set; } = "";
		public string ServiceType { get; set; } = "gemini";
		public string ModelName { get; set; } = "";
	}
}
