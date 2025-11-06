using Npgsql;
using System.Data;

namespace FinancialStatementService.DataAccess.DbConnectionFactory
{
	public class SqlConnectionFactory : IDbConnectionFactory
	{
		private readonly string _connectionString;

		public SqlConnectionFactory(IConfiguration configuration)
		{
			_connectionString = configuration.GetConnectionString("DefaultConnection");
		}

		public IDbConnection GetConnection()
		{
			return new NpgsqlConnection(_connectionString);
		}
	}
}
