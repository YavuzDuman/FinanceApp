using System.Data;

namespace FinancialNewsService.DataAccess.DbConnectionFactory
{
	public interface IDbConnectionFactory
	{
		IDbConnection GetConnection();
	}
}
