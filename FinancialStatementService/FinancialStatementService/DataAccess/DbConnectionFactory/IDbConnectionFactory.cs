using System.Data;

namespace FinancialStatementService.DataAccess.DbConnectionFactory
{
	public interface IDbConnectionFactory
	{
		IDbConnection GetConnection();
	}
}
