using Microsoft.EntityFrameworkCore;
using StockSensePro.Core.Entities;

namespace StockSensePro.Infrastructure.Data
{
    public class StockSenseProDbContext : DbContext
    {
        public StockSenseProDbContext(DbContextOptions<StockSenseProDbContext> options) : base(options)
        {
        }

        public DbSet<Stock> Stocks { get; set; }
        public DbSet<StockPrice> StockPrices { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Stock>(entity =>
            {
                entity.HasKey(e => e.Symbol);
                entity.Property(e => e.Symbol).HasMaxLength(10);
                entity.Property(e => e.Name).HasMaxLength(100);
                entity.Property(e => e.Exchange).HasMaxLength(50);
                entity.Property(e => e.Sector).HasMaxLength(50);
                entity.Property(e => e.Industry).HasMaxLength(100);
            });

            modelBuilder.Entity<StockPrice>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Symbol);
                entity.HasIndex(e => e.Date);
                entity.Property(e => e.Symbol).HasMaxLength(10);
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}
