using Microsoft.EntityFrameworkCore;
using StockSensePro.Core.Entities;
using StockSensePro.Core.Interfaces;
using StockSensePro.Infrastructure.Data;

namespace StockSensePro.Infrastructure.Data.Repositories
{
    public class DatabaseStockRepository : IStockRepository
    {
        private readonly StockSenseProDbContext _context;

        public DatabaseStockRepository(StockSenseProDbContext context)
        {
            _context = context;
        }

        public async Task<Stock?> GetBySymbolAsync(string symbol)
        {
            return await _context.Stocks
                .FirstOrDefaultAsync(s => s.Symbol == symbol);
        }

        public async Task<IEnumerable<Stock>> GetAllAsync()
        {
            return await _context.Stocks.ToListAsync();
        }

        public async Task AddAsync(Stock stock)
        {
            _context.Stocks.Add(stock);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Stock stock)
        {
            _context.Stocks.Update(stock);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(string symbol)
        {
            var stock = await _context.Stocks
                .FirstOrDefaultAsync(s => s.Symbol == symbol);
            
            if (stock != null)
            {
                _context.Stocks.Remove(stock);
                await _context.SaveChangesAsync();
            }
        }
    }
}
