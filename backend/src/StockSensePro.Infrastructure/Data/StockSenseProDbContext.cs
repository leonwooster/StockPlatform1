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
        public DbSet<TradingSignal> TradingSignals { get; set; }
        public DbSet<SignalPerformance> SignalPerformances { get; set; }

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

            modelBuilder.Entity<TradingSignal>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Symbol);
                entity.Property(e => e.Symbol).HasMaxLength(10);
                entity.Property(e => e.Strategy).HasMaxLength(100);
                entity.Property(e => e.SignalType).HasMaxLength(50);
                entity.Property(e => e.Status).HasMaxLength(50);

                entity.HasMany(e => e.Performances)
                    .WithOne(p => p.TradingSignal)
                    .HasForeignKey(p => p.TradingSignalId);
            });

            modelBuilder.Entity<SignalPerformance>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.EvaluatedAt);
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}
