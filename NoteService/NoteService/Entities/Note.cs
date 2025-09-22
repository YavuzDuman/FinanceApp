using Shared.Abstract;

namespace NoteService.Entities
{
	public class Note : IEntity
	{
		public int NoteId { get; set; }
		public int UserId { get; set; }
		public string StockSymbol { get; set; }
		public string Content { get; set; }
		public DateTime LastModifiedDate { get; set; }
	}
}
