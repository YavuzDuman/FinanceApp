using Microsoft.EntityFrameworkCore;
using WatchlistService.Entities;

namespace WatchlistService.DataAccess.Context
{
	public class WatchlistDbContext : DbContext
	{
		public WatchlistDbContext(DbContextOptions<WatchlistDbContext> options) : base(options) { }

		// Veritabanındaki tablolara karşılık gelen DbSet'ler
		public DbSet<Watchlist> Watchlists { get; set; }
		public DbSet<WatchlistItem> WatchlistItems { get; set; }

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{

			modelBuilder.Entity<WatchlistItem>()
				.HasOne(item => item.Watchlist) 
				.WithMany(w => w.Items) 
				.HasForeignKey(item => item.WatchlistId) 
				.OnDelete(DeleteBehavior.Cascade); 

			
			base.OnModelCreating(modelBuilder);
		}
	}
}
