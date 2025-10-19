using StockSensePro.Core.Entities;
using StockSensePro.Core.Interfaces;

namespace StockSensePro.Infrastructure.Data.Repositories
{
    public class InMemoryStockRepository : IStockRepository
    {
        private readonly List<Stock> _stocks;

        public InMemoryStockRepository()
        {
            _stocks = new List<Stock>
            {
                new Stock
                {
                    Symbol = "AAPL",
                    Name = "Apple Inc.",
                    Exchange = "NASDAQ",
                    Sector = "Technology",
                    Industry = "Consumer Electronics",
                    CurrentPrice = 175.00m,
                    PreviousClose = 173.50m,
                    Open = 174.25m,
                    High = 176.50m,
                    Low = 173.75m,
                    Volume = 45000000,
                    LastUpdated = DateTime.UtcNow
                },
                new Stock
                {
                    Symbol = "GOOGL",
                    Name = "Alphabet Inc.",
                    Exchange = "NASDAQ",
                    Sector = "Technology",
                    Industry = "Internet Content & Information",
                    CurrentPrice = 2800.00m,
                    PreviousClose = 2790.00m,
                    Open = 2795.00m,
                    High = 2810.00m,
                    Low = 2785.00m,
                    Volume = 1200000,
                    LastUpdated = DateTime.UtcNow
                }
            };
        }

        public async Task<Stock?> GetBySymbolAsync(string symbol)
        {
            return await Task.FromResult(_stocks.FirstOrDefault(s => s.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase)));
        }

        public async Task<IEnumerable<Stock>> GetAllAsync()
        {
            return await Task.FromResult(_stocks);
        }

        public async Task AddAsync(Stock stock)
        {
            _stocks.Add(stock);
            await Task.CompletedTask;
        }

        public async Task UpdateAsync(Stock stock)
        {
            var existingStock = _stocks.FirstOrDefault(s => s.Symbol.Equals(stock.Symbol, StringComparison.OrdinalIgnoreCase));
            if (existingStock != null)
            {
                _stocks.Remove(existingStock);
                _stocks.Add(stock);
            }
            await Task.CompletedTask;
        }

        public async Task DeleteAsync(string symbol)
        {
            var stock = _stocks.FirstOrDefault(s => s.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase));
            if (stock != null)
            {
                _stocks.Remove(stock);
            }
            await Task.CompletedTask;
        }
    }
}
