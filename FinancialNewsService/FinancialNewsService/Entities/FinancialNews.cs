using Shared.Abstract;

namespace FinancialNewsService.Entities
{
	public class FinancialNews : IEntity
	{
		public int Id { get; set; }
		public string Title { get; set; } = string.Empty;
		public string Content { get; set; } = string.Empty;
		public string Summary { get; set; } = string.Empty;
		public string Author { get; set; } = string.Empty;
		public DateTime PublishedDate { get; set; }
		public string SourceUrl { get; set; } = string.Empty;
		public string Category { get; set; } = string.Empty;
		public string Tags { get; set; } = string.Empty;
		public DateTime CreatedDate { get; set; }
		public DateTime UpdatedDate { get; set; }
	}
}
