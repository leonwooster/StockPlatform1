using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StockSensePro.Core.Entities;

namespace StockSensePro.Infrastructure.Data
{
    public static class DatabaseInitializer
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<StockSenseProDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
                .CreateLogger("DatabaseInitializer");

            try
            {
                await context.Database.EnsureCreatedAsync();

                if (!await context.Stocks.AnyAsync())
                {
                    var now = DateTime.UtcNow;

                    var stocks = new List<Stock>
                    {
                        new()
                        {
                            Symbol = "AAPL",
                            Name = "Apple Inc.",
                            Exchange = "NASDAQ",
                            Sector = "Technology",
                            Industry = "Consumer Electronics",
                            CurrentPrice = 189.87m,
                            PreviousClose = 188.06m,
                            Open = 189.20m,
                            High = 190.10m,
                            Low = 187.95m,
                            Volume = 53214567,
                            LastUpdated = now
                        },
                        new()
                        {
                            Symbol = "MSFT",
                            Name = "Microsoft Corporation",
                            Exchange = "NASDAQ",
                            Sector = "Technology",
                            Industry = "Software—Infrastructure",
                            CurrentPrice = 338.15m,
                            PreviousClose = 336.42m,
                            Open = 337.50m,
                            High = 339.75m,
                            Low = 335.80m,
                            Volume = 28123456,
                            LastUpdated = now
                        },
                        new()
                        {
                            Symbol = "GOOGL",
                            Name = "Alphabet Inc. Class A",
                            Exchange = "NASDAQ",
                            Sector = "Communication Services",
                            Industry = "Internet Content & Information",
                            CurrentPrice = 139.54m,
                            PreviousClose = 138.22m,
                            Open = 138.75m,
                            High = 140.10m,
                            Low = 137.98m,
                            Volume = 19234567,
                            LastUpdated = now
                        }
                    };

                    await context.Stocks.AddRangeAsync(stocks);
                    await context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while initializing the database.");
                throw;
            }
        }
    }
}
