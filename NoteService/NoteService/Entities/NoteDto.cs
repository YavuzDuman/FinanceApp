using Shared.Abstract;

namespace NoteService.Entities
{
	public record NoteDto : IDto
	{
		public string StockSymbol { get; set; }
		public string Content { get; set; }
	}
}
