using Dapper;
using Microsoft.AspNetCore.Connections;
using NoteService.DataAccess.ConnectionFactory;
using NoteService.Entities;

namespace NoteService.DataAccess
{
	public class NotesRepository : INotesRepository
	{
		private readonly IDbConnectionFactory _dbConnectionFactory;

		public NotesRepository(IDbConnectionFactory dbConnectionFactory)
		{
			_dbConnectionFactory = dbConnectionFactory;
		}

	public async Task<int> CreateNoteAsync(Note note)
	{
		try
		{
			using var connection = _dbConnectionFactory.GetConnection();
			var sql = @"INSERT INTO ""Notes"" (""UserId"", ""StockSymbol"", ""Content"", ""LastModifiedDate"") VALUES (@UserId, @StockSymbol, @Content, @LastModifiedDate) RETURNING ""NoteId"";";
			var result = await connection.ExecuteScalarAsync<int>(sql, note);
			Console.WriteLine($"CreateNoteAsync: Not başarıyla oluşturuldu. NoteId: {result}");
			return result;
		}
		catch (Exception ex)
		{
			Console.WriteLine($"CreateNoteAsync hatası: {ex.Message}");
			Console.WriteLine($"Stack trace: {ex.StackTrace}");
			if (ex.InnerException != null)
			{
				Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
			}
			throw;
		}
	}

	public async Task<bool> UpdateNoteAsync(Note note)
	{
		using var connection = _dbConnectionFactory.GetConnection();
		var sql = @"UPDATE ""Notes"" SET ""Content"" = @Content, ""LastModifiedDate"" = @LastModifiedDate WHERE ""NoteId"" = @NoteId;";
		var rowsAffected = await connection.ExecuteAsync(sql, note);
		return rowsAffected > 0;
	}

	public async Task<bool> DeleteNoteAsync(int noteId)
	{
		using var connection = _dbConnectionFactory.GetConnection();
		var sql = @"DELETE FROM ""Notes"" WHERE ""NoteId"" = @NoteId;";
		var rowsAffected = await connection.ExecuteAsync(sql, new { NoteId = noteId });
		return rowsAffected > 0;
	}

	public async Task<Note> GetNoteByIdAsync(int noteId)
	{
		using var connection = _dbConnectionFactory.GetConnection();
		var sql = @"SELECT * FROM ""Notes"" WHERE ""NoteId"" = @noteId";
		var note = await connection.QueryFirstOrDefaultAsync<Note>(sql, new { NoteId = noteId });
		if(note == null)
		{
			throw new KeyNotFoundException($"Note with ID {noteId} not found.");
		}
		return note;
	}

	public async Task<IEnumerable<Note>> GetUserNotesAsync(int userId)
	{
		try
		{
			using var connection = _dbConnectionFactory.GetConnection();
			var sql = @"SELECT * FROM ""Notes"" WHERE ""UserId"" = @UserId;";
			var notes = await connection.QueryAsync<Note>(sql, new { UserId = userId });
			var result = notes.ToList();
			Console.WriteLine($"GetUserNotesAsync: {result.Count} not bulundu (UserId: {userId})");
			return result;
		}
		catch (Exception ex)
		{
			Console.WriteLine($"GetUserNotesAsync hatası: {ex.Message}");
			Console.WriteLine($"Stack trace: {ex.StackTrace}");
			if (ex.InnerException != null)
			{
				Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
			}
			throw;
		}
	}

		
	}
}
