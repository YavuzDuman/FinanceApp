using PortfolioService.Entities.Dtos;
using Shared.Abstract;
using System.ComponentModel.DataAnnotations;

namespace PortfolioService.Entities.Concrete
{
	public class Portfolio : IEntity
	{
		[Key]
		public int Id { get; set; }
		public string Name { get; set; }
		public int UserId { get; set; } 
		public DateTime CreationDate { get; set; }

		public ICollection<PortfolioItem> PortfolioItems { get; set; }
	}
}