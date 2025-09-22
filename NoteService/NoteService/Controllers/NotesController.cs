using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NoteService.Business;
using NoteService.Entities;
using System.Security.Claims;

namespace NoteService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotesController : ControllerBase
    {
        private readonly INotesManager _notesManager;

		public NotesController(INotesManager notesManager)
		{
			_notesManager = notesManager;
		}
		private int GetUserIdFromToken()
		{
			var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

			if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
			{
				throw new InvalidOperationException("User ID claim is missing or invalid.");
			}

			return userId;
		}
		[HttpPost]
		public async Task<IActionResult> CreateNote([FromBody] NoteDto createDto)
		{
			var userId = GetUserIdFromToken();
			var noteId = await _notesManager.CreateNoteAsync(createDto, userId);
			return CreatedAtAction(nameof(GetNoteById), new { id = noteId }, null);
		}

		[HttpGet]
		public async Task<IActionResult> GetUserNotes()
		{
			var userId = GetUserIdFromToken();
			var notes = await _notesManager.GetUserNotesAsync(userId);
			return Ok(notes);
		}

		[HttpGet("{id}")]
		public async Task<IActionResult> GetNoteById(int id)
		{
			var userId = GetUserIdFromToken();
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
			var userId = GetUserIdFromToken();
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
			var userId = GetUserIdFromToken();
			var isDeleted = await _notesManager.DeleteNoteAsync(id, userId);

			if (!isDeleted)
			{
				return NotFound();
			}
			return NoContent();
		}


	}
}
