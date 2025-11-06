using AutoMapper;
using NoteService.Business;
using NoteService.DataAccess;
using NoteService.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

public class NotesManager : INotesManager
{
	private readonly INotesRepository _notesRepository;
	private readonly IMapper _mapper;

	public NotesManager(INotesRepository notesRepository, IMapper mapper)
	{
		_notesRepository = notesRepository;
		_mapper = mapper;
	}

	public async Task<int> CreateNoteAsync(NoteDto dto, int userId)
	{
		var note = _mapper.Map<Note>(dto);
		note.UserId = userId;
		note.LastModifiedDate = DateTime.UtcNow;

		return await _notesRepository.CreateNoteAsync(note);
	}

	public async Task<bool> DeleteNoteAsync(int noteId, int userId)
	{
		var existingNote = await _notesRepository.GetNoteByIdAsync(noteId);
		if (existingNote == null || existingNote.UserId != userId)
		{
			return false; 
		}
		return await _notesRepository.DeleteNoteAsync(noteId);
	}

	public async Task<bool> UpdateNoteAsync(int noteId, NoteDto dto, int userId)
	{
		var existingNote = await _notesRepository.GetNoteByIdAsync(noteId);
		if (existingNote == null || existingNote.UserId != userId)
		{
			return false; // Not bulunamadı veya kullanıcı yetkili değil
		}
		_mapper.Map(dto, existingNote);
		existingNote.LastModifiedDate = DateTime.UtcNow;
		return await _notesRepository.UpdateNoteAsync(existingNote);
	}

	public async Task<Note> GetNoteByIdAsync(int noteId, int userId)
	{
		var note = await _notesRepository.GetNoteByIdAsync(noteId);

		// Güvenlik: Kullanıcı sadece kendi notuna erişebilmeli
		if (note == null || note.UserId != userId)
		{
			return null;
		}

		return note;
	}

	public async Task<IEnumerable<Note>> GetUserNotesAsync(int userId)
	{
		return await _notesRepository.GetUserNotesAsync(userId);
	}

	public async Task<IEnumerable<Note>> GetUserNotesByStockAsync(string stockSymbol, int userId)
	{
		var allNotes = await _notesRepository.GetUserNotesAsync(userId);
		return allNotes.Where(n => n.StockSymbol.Equals(stockSymbol, StringComparison.OrdinalIgnoreCase));
	}

}