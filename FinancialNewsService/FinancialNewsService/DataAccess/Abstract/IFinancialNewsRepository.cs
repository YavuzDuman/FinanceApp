using FinancialNewsService.Entities;

namespace FinancialNewsService.DataAccess.Abstract
{
	public interface IFinancialNewsRepository
	{
		Task<List<FinancialNews>> GetAllAsync();
		Task<FinancialNews?> GetByIdAsync(int id);
		Task<FinancialNews?> GetByUrlAsync(string url);
		Task InsertAsync(FinancialNews news);
		Task UpdateAsync(FinancialNews news);
		Task DeleteAllAsync();
		Task DeleteByIdAsync(int id);
	}
}
