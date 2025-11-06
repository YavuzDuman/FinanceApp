using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NoteService.Business;
using NoteService.Entities;
using System.Security.Claims;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.Authorization;
using Shared.Extensions;
using Shared.Helpers;

namespace NoteService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Güvenlik için geri eklendi - çift katmanlı koruma
    public class NotesController : ControllerBase
    {
        private readonly INotesManager _notesManager;
        private readonly IMemoryCache _cache;

		public NotesController(INotesManager notesManager, IMemoryCache cache)
		{
			_notesManager = notesManager;
			_cache = cache;
		}
		
	[HttpPost]
	public async Task<IActionResult> CreateNote([FromBody] NoteDto createDto)
	{
		try
		{
			var userId = await UserContextHelper.GetUserIdFromTokenCachedAsync(HttpContext, _cache);
			var noteId = await _notesManager.CreateNoteAsync(createDto, userId);
			return CreatedAtAction(nameof(GetNoteById), new { id = noteId }, null);
		}
		catch (Exception ex)
		{
			Console.WriteLine($"CreateNote hatası: {ex.Message}");
			Console.WriteLine($"Stack trace: {ex.StackTrace}");
			if (ex.InnerException != null)
			{
				Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
			}
			return StatusCode(500, new { message = "Not eklenirken bir hata oluştu.", error = ex.Message });
		}
	}

	[HttpGet]
	public async Task<IActionResult> GetUserNotes()
	{
		try
		{
			var userId = await UserContextHelper.GetUserIdFromTokenCachedAsync(HttpContext, _cache);
			var notes = await _notesManager.GetUserNotesAsync(userId);
			return Ok(notes);
		}
		catch (Exception ex)
		{
			Console.WriteLine($"GetUserNotes hatası: {ex.Message}");
			Console.WriteLine($"Stack trace: {ex.StackTrace}");
			if (ex.InnerException != null)
			{
				Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
			}
			return StatusCode(500, new { message = "Notlar yüklenirken bir hata oluştu.", error = ex.Message });
		}
	}

	// GET /api/notes/stock/{symbol} - Hisseye özel notları getir
	[HttpGet("stock/{symbol}")]
	public async Task<IActionResult> GetNotesByStock(string symbol)
	{
		try
		{
			var userId = await UserContextHelper.GetUserIdFromTokenCachedAsync(HttpContext, _cache);
			var notes = await _notesManager.GetUserNotesByStockAsync(symbol, userId);
			return Ok(notes);
		}
		catch (Exception ex)
		{
			Console.WriteLine($"GetNotesByStock hatası: {ex.Message}");
			Console.WriteLine($"Stack trace: {ex.StackTrace}");
			if (ex.InnerException != null)
			{
				Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
			}
			return StatusCode(500, new { message = "Notlar yüklenirken bir hata oluştu.", error = ex.Message });
		}
	}

		[HttpGet("{id}")]
		public async Task<IActionResult> GetNoteById(int id)
		{
			var userId = await UserContextHelper.GetUserIdFromTokenCachedAsync(HttpContext, _cache);
			var note = await _notesManager.GetNoteByIdAsync(id, userId);

			if (note == null)
			{
				return NotFound();
			}

			return Ok(note);
		}

		[HttpPut("{id}")]
		public async Task<IActionResult> UpdateNote(int id, [FromBody] NoteDto updateDto)
		{
			var userId = await UserContextHelper.GetUserIdFromTokenCachedAsync(HttpContext, _cache);	
			var isUpdated = await _notesManager.UpdateNoteAsync(id, updateDto, userId);

			if (!isUpdated)
			{
				return NotFound();
			}
			return NoContent();
		}

		[HttpDelete("{id}")]
		public async Task<IActionResult> DeleteNote(int id)
		{
			var userId = await UserContextHelper.GetUserIdFromTokenCachedAsync(HttpContext, _cache);
			var isDeleted = await _notesManager.DeleteNoteAsync(id, userId);

			if (!isDeleted)
			{
				return NotFound();
			}
			return NoContent();
		}


	}
}
