using System.Data;
using System.Data.SqlClient;

namespace FinancialNewsService.DataAccess.DbConnectionFactory
{
	public class SqlConnectionFactory : IDbConnectionFactory
	{
		private readonly string _connectionString;

		public SqlConnectionFactory(IConfiguration configuration)
		{
			_connectionString = configuration.GetConnectionString("DefaultConnection")!;
		}

		public IDbConnection GetConnection()
		{
			return new SqlConnection(_connectionString);
		}
	}
}
