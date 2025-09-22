using Shared.Abstract;

namespace PortfolioService.Entities.Dtos
{
	public class AddItemToPortfolioDto : IDto
	{
		public string Symbol { get; set; }
		public decimal PurchasePrice { get; set; }
		public int Quantity { get; set; }
	}
}
