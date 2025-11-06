using PortfolioService.DataAccess.Abstract;
using PortfolioService.Entities.Concrete;
using PortfolioService.DataAccess.Context;
using Microsoft.EntityFrameworkCore;

namespace PortfolioService.DataAccess.Concrete
{
	public class PortfolioRepository : IPortfolioRepository
	{
		private readonly PortfolioDatabaseContext _context;

		public PortfolioRepository(PortfolioDatabaseContext context)
		{
			_context = context;
		}

		//Portfolio Methodlari

		public async Task<List<Portfolio>> GetAllPortfoliosAsync()
		{
			return await _context.Portfolios
				.AsNoTracking() // ⚡ Change tracking'i kapat - read-only sorgu
				.Include(p => p.PortfolioItems)
				.ToListAsync();
		}

		public async Task<Portfolio> GetPortfolioByIdAsync(int portfolioId)
		{
			var portfolio = await _context.Portfolios
				.AsNoTracking() // ⚡ Change tracking'i kapat - read-only sorgu
				.Include(p=> p.PortfolioItems)
				.FirstOrDefaultAsync(p => p.Id == portfolioId)
				;
			return portfolio;
		}

		public async Task<List<Portfolio>> GetAllPortfoliosByUserIdAsync(int userId)
		{
			return await _context.Portfolios
				.AsNoTracking() // ⚡ Change tracking'i kapat - read-only sorgu
				.Where(p => p.UserId == userId)
				.Include(p=> p.PortfolioItems)
				.ToListAsync();
		}

		public async Task AddPortfolioAsync(Portfolio portfolio)
		{
			await _context.Portfolios.AddAsync(portfolio);
			await _context.SaveChangesAsync();
		}

		public async Task UpdatePortfolioAsync(Portfolio portfolio)
		{
			_context.Portfolios.Update(portfolio);
			await _context.SaveChangesAsync();
		}
		public async Task DeletePortfolioAsync(int portfolioId)
		{
			var portfolioToDelete = await _context.Portfolios.FindAsync(portfolioId);
			if (portfolioToDelete != null)
			{
				_context.Portfolios.Remove(portfolioToDelete);
				await _context.SaveChangesAsync();
			}
		}

		//PortfolioItem Methodlari

		public async Task<PortfolioItem> GetPortfolioItemByIdAsync(int portfolioItemId)
		{
			return await _context.PortfolioItems.FindAsync(portfolioItemId);
		}

		public async Task<List<PortfolioItem>> GetPortfolioItemsByPortfolioIdAsync(int portfolioId)
		{
			return await _context.PortfolioItems
				.Where(pi => pi.PortfolioId == portfolioId)
				.ToListAsync();
		}

		public async Task AddPortfolioItemAsync(PortfolioItem item)
		{
			await _context.PortfolioItems.AddAsync(item);
			await _context.SaveChangesAsync(); 
		}

		public async Task DeletePortfolioItemAsync(int portfolioItemId)
		{
			var itemToDelete = await _context.PortfolioItems.FindAsync(portfolioItemId);
			if (itemToDelete != null)
			{
				_context.PortfolioItems.Remove(itemToDelete);
				await _context.SaveChangesAsync();
			}
		}

		public async Task UpdatePortfolioItemAsync(PortfolioItem item)
		{
			_context.PortfolioItems.Update(item);
			await _context.SaveChangesAsync();
		}

		public async Task UpdateCurrentPriceBySymbolAsync(string symbol, decimal currentPrice)
		{
			var items = await _context.PortfolioItems
				.Where(pi => pi.Symbol == symbol)
				.ToListAsync();
			if (items.Count == 0) return;
			foreach (var item in items)
			{
				item.CurrentPrice = currentPrice;
			}
			_context.PortfolioItems.UpdateRange(items);
			await _context.SaveChangesAsync();
		}
	}
}
