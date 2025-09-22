using FinancialNewsService.Entities;

namespace FinancialNewsService.Business.Abstract
{
	public interface IFinancialNewsManager
	{
		Task<List<FinancialNewsDto>> GetAllNewsAsync();
		Task<FinancialNewsDto?> GetNewsByIdAsync(int id);
		Task<List<FinancialNewsDto>> FetchAndSaveFinancialNewsAsync();
		Task DeleteAllNewsAsync();
	}
}
