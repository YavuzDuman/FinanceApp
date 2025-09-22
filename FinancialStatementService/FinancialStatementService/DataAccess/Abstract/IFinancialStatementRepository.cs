using FinancialStatementService.Entities;

namespace FinancialStatementService.DataAccess.Abstract
{
	public interface IFinancialStatementRepository
	{
		Task<FinancialStatement?> GetSymbolByNameAsync(string symbol);
		Task<List<FinancialStatement>> GetAllSymbolsAsync();
		Task InsertAsync(FinancialStatement statement);
		Task UpdateAsync(FinancialStatement statement);
		Task DeleteAllAsync();
	}
}
