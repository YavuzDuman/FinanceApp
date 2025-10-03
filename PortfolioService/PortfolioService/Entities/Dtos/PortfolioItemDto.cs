using Shared.Abstract;

namespace PortfolioService.Entities.Dtos
{
	public class PortfolioItemDto : IDto
	{
		public string Symbol { get; set; }
		public decimal AverageCost { get; set; }
		public int Quantity { get; set; }
		public decimal CurrentPrice { get; set; }
	}
}
