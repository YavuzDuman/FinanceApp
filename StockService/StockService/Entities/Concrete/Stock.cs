using System.ComponentModel.DataAnnotations.Schema;
using StockService.Entities.Abstract;

namespace StockService.Entities.Concrete
{
	public class Stock : BaseEntity
	{
		public string Symbol { get; set; } = string.Empty;
		public string CompanyName { get; set; } = string.Empty;

		[Column(TypeName = "decimal(18, 2)")]
		public decimal CurrentPrice { get; set; }

		[Column(TypeName = "decimal(18, 2)")]
		public decimal Change { get; set; }

		[Column(TypeName = "decimal(18, 2)")]
		public decimal ChangePercent { get; set; }

		[Column(TypeName = "decimal(18, 2)")]
		public decimal OpenPrice { get; set; }

		[Column(TypeName = "decimal(18, 2)")]
		public decimal HighPrice { get; set; }

		[Column(TypeName = "decimal(18, 2)")]
		public decimal LowPrice { get; set; }

		public long Volume { get; set; }

		public DateTime LastUpdate { get; set; }
	}
}