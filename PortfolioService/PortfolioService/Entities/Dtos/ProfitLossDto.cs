using Shared.Abstract;

namespace PortfolioService.Entities.Dtos
{
	public class ProfitLossDto : IDto
	{
		public string Symbol { get; set; }
		public decimal ProfitLossValue { get; set; }
	}
}
