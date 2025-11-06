using Shared.Abstract;

namespace WatchlistService.Entities.Dtos
{
	public record StockDto : IDto
	{
		public string Symbol { get; set; }
		public decimal CurrentPrice { get; set; }
	}
}
