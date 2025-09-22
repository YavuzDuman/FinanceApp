using System.Data;

namespace NoteService.DataAccess.ConnectionFactory
{
	public interface IDbConnectionFactory
	{
		IDbConnection GetConnection();
	}
}
