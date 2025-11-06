using Shared.Abstract;

namespace WatchlistService.Entities.Dtos
{
	public record WatchlistItemResponseDto : IDto
	{
		public int Id { get; set; }
		public string Symbol { get; set; }
		public string? Note { get; set; }
		public DateTime EntryDate { get; set; }
		public decimal CurrentPrice { get; set; }
	}
}
