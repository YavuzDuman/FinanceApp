using Microsoft.EntityFrameworkCore;
using PortfolioService.Entities.Concrete;

namespace PortfolioService.DataAccess.Context
{
	public class PortfolioDatabaseContext : DbContext
	{
		public PortfolioDatabaseContext(DbContextOptions<PortfolioDatabaseContext> options)
			: base(options)
		{
		}

		public DbSet<Portfolio> Portfolios { get; set; }
		public DbSet<PortfolioItem> PortfolioItems { get; set; }

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{

			modelBuilder.Entity<PortfolioItem>()
				.Property(pi => pi.AverageCost)
				.HasPrecision(18, 6);

			modelBuilder.Entity<Portfolio>()
				.HasMany(p => p.PortfolioItems)
				.WithOne(pi => pi.Portfolio)
				.HasForeignKey(pi => pi.PortfolioId);
		}
	}
}