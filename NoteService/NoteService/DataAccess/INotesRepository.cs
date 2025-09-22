using NoteService.Entities;

namespace NoteService.DataAccess
{
	public interface INotesRepository
	{
		Task<int> CreateNoteAsync(Note note);
		Task<IEnumerable<Note>> GetUserNotesAsync(int userId);
		Task<Note> GetNoteByIdAsync(int noteId);
		Task<bool> UpdateNoteAsync(Note note);
		Task<bool> DeleteNoteAsync(int noteId);
	}
}
