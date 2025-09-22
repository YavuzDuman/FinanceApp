using Shared.Abstract;

namespace FinancialStatementService.Entities
{
	public class FinancialStatementDto : IDto
	{
		public string StockSymbol { get; set; }
		public string CompanyName { get; set; } 
		public string Type { get; set; }
		public DateTime StatementDate { get; set; }
		public string Data { get; set; }
		public DateTime? AnnouncementDate { get; set; }
		public decimal? NetProfitChangeRate { get; set; }
	}
}
