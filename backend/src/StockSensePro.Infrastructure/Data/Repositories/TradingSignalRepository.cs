using Microsoft.EntityFrameworkCore;
using StockSensePro.Core.Entities;
using StockSensePro.Core.Interfaces;

namespace StockSensePro.Infrastructure.Data.Repositories
{
    public class TradingSignalRepository : ITradingSignalRepository
    {
        private readonly StockSenseProDbContext _context;

        public TradingSignalRepository(StockSenseProDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(TradingSignal signal, CancellationToken cancellationToken = default)
        {
            await _context.TradingSignals.AddAsync(signal, cancellationToken);
        }

        public async Task AddRangeAsync(IEnumerable<TradingSignal> signals, CancellationToken cancellationToken = default)
        {
            await _context.TradingSignals.AddRangeAsync(signals, cancellationToken);
        }

        public async Task<IReadOnlyList<TradingSignal>> GetBySymbolAsync(string symbol, CancellationToken cancellationToken = default)
        {
            return await _context.TradingSignals
                .AsNoTracking()
                .Where(s => s.Symbol == symbol)
                .OrderByDescending(s => s.GeneratedAt)
                .ToListAsync(cancellationToken);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return _context.SaveChangesAsync(cancellationToken);
        }
    }
}
