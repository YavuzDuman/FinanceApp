using Dapper;
using FinancialStatementService.DataAccess.Abstract;
using FinancialStatementService.DataAccess.DbConnectionFactory;
using FinancialStatementService.Entities;

namespace FinancialStatementService.DataAccess.Concrete
{
	public class FinancialStatementRepository : IFinancialStatementRepository
	{
		private readonly IDbConnectionFactory _connectionFactory;

		public FinancialStatementRepository(IDbConnectionFactory connectionFactory)
		{
			_connectionFactory = connectionFactory;
		}

		public async Task<List<FinancialStatement>> GetAllSymbolsAsync()
		{
			var sql = "SELECT * FROM FinancialStatements";
			using var connection = _connectionFactory.GetConnection();
			var symbols = await connection.QueryAsync<FinancialStatement>(sql);
			return symbols.ToList();
		}

		public async Task<FinancialStatement?> GetSymbolByNameAsync(string symbol)
		{
			var sql = "SELECT * FROM FinancialStatements WHERE StockSymbol = @Symbol";
			using var connection = _connectionFactory.GetConnection();
			return await connection.QueryFirstOrDefaultAsync<FinancialStatement>(sql, new { Symbol = symbol });
		}

		public async Task InsertAsync(FinancialStatement statement)
		{
			var sql = @"INSERT INTO FinancialStatements 
				(StockSymbol, CompanyName, StatementDate, Type, Data, AnnouncementDate, NetProfitChangeRate, UpdatedDate) 
				VALUES (@StockSymbol, @CompanyName, @StatementDate, @Type, @Data, @AnnouncementDate, @NetProfitChangeRate, @UpdatedDate)";
			using var connection = _connectionFactory.GetConnection();
			await connection.ExecuteAsync(sql, statement);
		}

		public async Task UpdateAsync(FinancialStatement statement)
		{
			var sql = @"UPDATE FinancialStatements SET 
				CompanyName = @CompanyName,
				Data = @Data, 
				AnnouncementDate = @AnnouncementDate, 
				NetProfitChangeRate = @NetProfitChangeRate, 
				UpdatedDate = @UpdatedDate 
				WHERE Id = @Id";
			using var connection = _connectionFactory.GetConnection();
			await connection.ExecuteAsync(sql, statement);
		}

		public async Task DeleteAllAsync()
		{
			var sql = "DELETE FROM FinancialStatements; DBCC CHECKIDENT('FinancialStatements', RESEED, 0)";
			using var connection = _connectionFactory.GetConnection();
			await connection.ExecuteAsync(sql);
		}

		
	}
}
