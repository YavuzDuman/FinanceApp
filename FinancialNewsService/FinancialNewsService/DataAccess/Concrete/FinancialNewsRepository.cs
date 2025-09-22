using Dapper;
using FinancialNewsService.DataAccess.Abstract;
using FinancialNewsService.DataAccess.DbConnectionFactory;
using FinancialNewsService.Entities;

namespace FinancialNewsService.DataAccess.Concrete
{
	public class FinancialNewsRepository : IFinancialNewsRepository
	{
		private readonly IDbConnectionFactory _connectionFactory;

		public FinancialNewsRepository(IDbConnectionFactory connectionFactory)
		{
			_connectionFactory = connectionFactory;
		}

		public async Task<List<FinancialNews>> GetAllAsync()
		{
			var sql = "SELECT * FROM FinancialNews ORDER BY PublishedDate DESC";
			using var connection = _connectionFactory.GetConnection();
			var news = await connection.QueryAsync<FinancialNews>(sql);
			return news.ToList();
		}

		public async Task<FinancialNews?> GetByIdAsync(int id)
		{
			var sql = "SELECT * FROM FinancialNews WHERE Id = @Id";
			using var connection = _connectionFactory.GetConnection();
			return await connection.QueryFirstOrDefaultAsync<FinancialNews>(sql, new { Id = id });
		}

		public async Task<FinancialNews?> GetByUrlAsync(string url)
		{
			var sql = "SELECT * FROM FinancialNews WHERE SourceUrl = @Url";
			using var connection = _connectionFactory.GetConnection();
			return await connection.QueryFirstOrDefaultAsync<FinancialNews>(sql, new { Url = url });
		}

		public async Task InsertAsync(FinancialNews news)
		{
			var sql = @"INSERT INTO FinancialNews 
				(Title, Content, Summary, Author, PublishedDate, SourceUrl, Category, Tags, CreatedDate, UpdatedDate) 
				VALUES (@Title, @Content, @Summary, @Author, @PublishedDate, @SourceUrl, @Category, @Tags, @CreatedDate, @UpdatedDate)";
			using var connection = _connectionFactory.GetConnection();
			await connection.ExecuteAsync(sql, news);
		}

		public async Task UpdateAsync(FinancialNews news)
		{
			var sql = @"UPDATE FinancialNews SET 
				Title = @Title,
				Content = @Content,
				Summary = @Summary,
				Author = @Author,
				PublishedDate = @PublishedDate,
				Category = @Category,
				Tags = @Tags,
				UpdatedDate = @UpdatedDate 
				WHERE Id = @Id";
			using var connection = _connectionFactory.GetConnection();
			await connection.ExecuteAsync(sql, news);
		}

		public async Task DeleteAllAsync()
		{
			var sql = "DELETE FROM FinancialNews; DBCC CHECKIDENT('FinancialNews', RESEED, 0)";
			using var connection = _connectionFactory.GetConnection();
			await connection.ExecuteAsync(sql);
		}

		public async Task DeleteByIdAsync(int id)
		{
			var sql = "DELETE FROM FinancialNews WHERE Id = @Id";
			using var connection = _connectionFactory.GetConnection();
			await connection.ExecuteAsync(sql, new { Id = id });
		}
	}
}
