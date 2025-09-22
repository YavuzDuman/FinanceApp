using FinancialStatementService.Entities;

namespace FinancialStatementService.Business.Abstract
{
	public interface IFinancialStatementManager
	{
		Task<List<FinancialStatement>> FetchAndSaveFinancialStatementsAsync();
		Task<FinancialStatementDto?> GetSymbolByNameAsync(string symbol);
		Task<List<FinancialStatementDto>> GetAllSymbolsAsync();
	}
}
