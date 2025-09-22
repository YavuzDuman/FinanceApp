using Shared.Abstract;

namespace PortfolioService.Entities.Dtos
{
	public class UpdatePortfolioNameDto : IDto
	{
		public string NewName { get; set; }
	}
}
