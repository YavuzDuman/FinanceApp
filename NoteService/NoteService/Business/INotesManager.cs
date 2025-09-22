using NoteService.Entities;

namespace NoteService.Business
{
	public interface INotesManager
	{
		Task<int> CreateNoteAsync(NoteDto dto,int userId);
		Task<IEnumerable<Note>> GetUserNotesAsync(int userId);
		Task<Note> GetNoteByIdAsync(int noteId,int userId);
		Task<bool> UpdateNoteAsync(int noteId, NoteDto dto, int userId);
		Task<bool> DeleteNoteAsync(int noteId, int userId);
	}
}
