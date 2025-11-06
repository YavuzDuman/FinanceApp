using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Shared.Helpers;
using WatchlistService.Business.Abstract;
using WatchlistService.Entities;
using WatchlistService.Entities.Dtos;

namespace WatchlistService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class WatchlistController : ControllerBase
    {
        private readonly IWatchlistManager _watchlistManager;
		private readonly IMemoryCache _cache;
		public WatchlistController(IWatchlistManager watchlistManager, IMemoryCache cache)
		{
			_watchlistManager = watchlistManager;
			_cache = cache;
		}

		[HttpPost]
		public async Task<IActionResult> CreateWatchlist([FromBody] CreateListDto createDto)
		{
			try
			{
				var userId = await UserContextHelper.GetUserIdFromTokenCachedAsync(HttpContext, _cache);
				var list = await _watchlistManager.AddWatchlistAsync(userId, createDto.ListName, createDto.Description);
				return CreatedAtAction(nameof(GetWatchlistById), new { id = list.Id }, new { message = "Başarılı şekilde liste oluşturuldu", id = list.Id });
			}
			catch (Exception ex)
			{
				return BadRequest(new { message = ex.Message });
			}
		}

		[HttpGet]
		public async Task<IActionResult> GetAllWatchlists()
		{
			try
			{
				var userId = await UserContextHelper.GetUserIdFromTokenCachedAsync(HttpContext, _cache);
				var watchlists = await _watchlistManager.GetAllByUserIdAsync(userId);
				
				var response = watchlists.Select(w => new WatchlistResponseDto
				{
					Id = w.Id,
					ListName = w.ListName,
					Description = w.Description,
					CreatedAt = w.CreatedAt,
					Items = w.Items.Select(i => new WatchlistItemResponseDto
					{
						Id = i.Id,
						Symbol = i.Symbol,
						Note = i.Note,
						EntryDate = i.EntryDate,
						CurrentPrice = i.CurrentPrice
					}).ToList()
				}).ToList();

				return Ok(response);
			}
			catch (Exception ex)
			{
				return BadRequest(new { message = ex.Message });
			}
		}

		[HttpGet("{id}")]
		public async Task<IActionResult> GetWatchlistById(int id)
		{
			try
			{
				var userId = await UserContextHelper.GetUserIdFromTokenCachedAsync(HttpContext, _cache);
				var watchlist = await _watchlistManager.GetWatchlistWithItemsAsync(id);

				if (watchlist == null)
				{
					return NotFound(new { message = "Watchlist bulunamadı" });
				}

				if (watchlist.UserId != userId)
				{
					return Forbid();
				}

				var response = new WatchlistResponseDto
				{
					Id = watchlist.Id,
					ListName = watchlist.ListName,
					Description = watchlist.Description,
					CreatedAt = watchlist.CreatedAt,
					Items = watchlist.Items.Select(i => new WatchlistItemResponseDto
					{
						Id = i.Id,
						Symbol = i.Symbol,
						Note = i.Note,
						EntryDate = i.EntryDate,
						CurrentPrice = i.CurrentPrice
					}).ToList()
				};

				return Ok(response);
			}
			catch (Exception ex)
			{
				return BadRequest(new { message = ex.Message });
			}
		}

		[HttpPut("{id}")]
		public async Task<IActionResult> UpdateWatchlist(int id, [FromBody] UpdateWatchlistDto updateDto)
		{
			try
			{
				var userId = await UserContextHelper.GetUserIdFromTokenCachedAsync(HttpContext, _cache);
				var updatedWatchlist = await _watchlistManager.UpdateWatchlistAsync(id, userId, updateDto.ListName, updateDto.Description);
				
				var response = new WatchlistResponseDto
				{
					Id = updatedWatchlist.Id,
					ListName = updatedWatchlist.ListName,
					Description = updatedWatchlist.Description,
					CreatedAt = updatedWatchlist.CreatedAt,
					Items = updatedWatchlist.Items.Select(i => new WatchlistItemResponseDto
					{
						Id = i.Id,
						Symbol = i.Symbol,
						Note = i.Note,
						EntryDate = i.EntryDate,
						CurrentPrice = i.CurrentPrice
					}).ToList()
				};

				return Ok(new { message = "Watchlist başarıyla güncellendi", watchlist = response });
			}
			catch (UnauthorizedAccessException ex)
			{
				return Forbid();
			}
			catch (Exception ex)
			{
				return BadRequest(new { message = ex.Message });
			}
		}

		[HttpDelete("{id}")]
		public async Task<IActionResult> DeleteWatchlist(int id)
		{
			try
			{
				var userId = await UserContextHelper.GetUserIdFromTokenCachedAsync(HttpContext, _cache);
				await _watchlistManager.DeleteWatchlistAsync(id, userId);
				return NoContent();
			}
			catch (UnauthorizedAccessException ex)
			{
				return Forbid();
			}
			catch (Exception ex)
			{
				return BadRequest(new { message = ex.Message });
			}
		}

		[HttpPost("{watchlistId}/items")]
		public async Task<IActionResult> AddItemToWatchlist(int watchlistId, [FromBody] AddItemToWatchlistDto addItemDto)
		{
			try
			{
				var userId = await UserContextHelper.GetUserIdFromTokenCachedAsync(HttpContext, _cache);
				var item = await _watchlistManager.AddItemToWatchlistAsync(watchlistId, addItemDto.Symbol, userId, HttpContext, addItemDto.Note);

				var response = new WatchlistItemResponseDto
				{
					Id = item.Id,
					Symbol = item.Symbol,
					Note = item.Note,
					EntryDate = item.EntryDate,
					CurrentPrice = item.CurrentPrice
				};

				return CreatedAtAction(nameof(GetItemById), new { watchlistId = watchlistId, itemId = item.Id }, new { message = "Item başarıyla eklendi", item = response });
			}
			catch (UnauthorizedAccessException ex)
			{
				return Forbid();
			}
			catch (InvalidOperationException ex)
			{
				return Conflict(new { message = ex.Message });
			}
			catch (Exception ex)
			{
				return BadRequest(new { message = ex.Message });
			}
		}

		[HttpGet("{watchlistId}/items/{itemId}")]
		public async Task<IActionResult> GetItemById(int watchlistId, int itemId)
		{
			try
			{
				var userId = await UserContextHelper.GetUserIdFromTokenCachedAsync(HttpContext, _cache);
				var watchlist = await _watchlistManager.GetWatchlistWithItemsAsync(watchlistId);

				if (watchlist == null || watchlist.UserId != userId)
				{
					return Forbid();
				}

				var item = await _watchlistManager.GetItemByIdAsync(itemId);
				if (item == null || item.WatchlistId != watchlistId)
				{
					return NotFound(new { message = "Item bulunamadı" });
				}

				var response = new WatchlistItemResponseDto
				{
					Id = item.Id,
					Symbol = item.Symbol,
					Note = item.Note,
					EntryDate = item.EntryDate,
					CurrentPrice = item.CurrentPrice
				};

				return Ok(response);
			}
			catch (Exception ex)
			{
				return BadRequest(new { message = ex.Message });
			}
		}

		[HttpPut("{watchlistId}/items/{itemId}")]
		public async Task<IActionResult> UpdateWatchlistItem(int watchlistId, int itemId, [FromBody] UpdateWatchlistItemDto updateDto)
		{
			try
			{
				var userId = await UserContextHelper.GetUserIdFromTokenCachedAsync(HttpContext, _cache);
				var updatedItem = await _watchlistManager.UpdateWatchlistItemAsync(itemId, updateDto.Note, userId);

				var response = new WatchlistItemResponseDto
				{
					Id = updatedItem.Id,
					Symbol = updatedItem.Symbol,
					Note = updatedItem.Note,
					EntryDate = updatedItem.EntryDate,
					CurrentPrice = updatedItem.CurrentPrice
				};

				return Ok(new { message = "Item başarıyla güncellendi", item = response });
			}
			catch (UnauthorizedAccessException ex)
			{
				return Forbid();
			}
			catch (Exception ex)
			{
				return BadRequest(new { message = ex.Message });
			}
		}

		[HttpDelete("{watchlistId}/items/{itemId}")]
		public async Task<IActionResult> RemoveItemFromWatchlist(int watchlistId, int itemId)
		{
			try
			{
				var userId = await UserContextHelper.GetUserIdFromTokenCachedAsync(HttpContext, _cache);
				await _watchlistManager.RemoveItemFromWatchlistAsync(itemId, userId);
				return NoContent();
			}
			catch (UnauthorizedAccessException ex)
			{
				return Forbid();
			}
			catch (Exception ex)
			{
				return BadRequest(new { message = ex.Message });
			}
		}
	}
}
