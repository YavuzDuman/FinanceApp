using Shared.Abstract;

namespace PortfolioService.Entities.Dtos
{
	public record ProfitLossDto : IDto
	{
		public decimal TotalProfitLoss { get; set; }
		public decimal TotalProfitLossPercentage { get; set; }
	}
}
