namespace StockService.Entities.Abstract
{
	public abstract class BaseEntity
	{
		public int Id { get; set; }
		public DateTime InsertDate { get; set; } = DateTime.UtcNow;
	}
}
