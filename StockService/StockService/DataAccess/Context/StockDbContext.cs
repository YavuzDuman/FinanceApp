using Microsoft.EntityFrameworkCore;
using StockService.Entities.Concrete;

namespace StockService.DataAccess.Context
{
	public class StockDbContext : DbContext
	{
		public StockDbContext(DbContextOptions<StockDbContext> options) : base(options)
		{
		}

		public DbSet<Stock> Stocks { get; set; }

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<Stock>().HasKey(s => s.Id);
			modelBuilder.Entity<Stock>().Property(s => s.Symbol).IsRequired().HasMaxLength(10);
			modelBuilder.Entity<Stock>().HasIndex(s => s.Symbol).IsUnique();
		}
	}
}
