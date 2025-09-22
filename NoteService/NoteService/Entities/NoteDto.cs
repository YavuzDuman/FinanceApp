using Shared.Abstract;

namespace NoteService.Entities
{
	public class NoteDto : IDto
	{
		public string StockSymbol { get; set; }
		public string Content { get; set; }
	}
}
