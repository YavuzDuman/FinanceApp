using Shared.Abstract;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PortfolioService.Entities.Concrete
{
	public class PortfolioItem : IEntity
	{
		[Key]
		public int Id { get; set; }
		public string Symbol { get; set; } 
		public decimal AverageCost { get; set; }
		public int Quantity { get; set; }
		public DateTime PurchaseDate { get; set; }
		[ForeignKey("Portfolio")]
		public int PortfolioId { get; set; }
		public decimal CurrentPrice { get; set; }


		public Portfolio Portfolio { get; set; }
	}
}