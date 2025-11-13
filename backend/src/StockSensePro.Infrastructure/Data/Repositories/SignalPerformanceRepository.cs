using Microsoft.EntityFrameworkCore;
using StockSensePro.Core.Entities;
using StockSensePro.Core.Interfaces;

namespace StockSensePro.Infrastructure.Data.Repositories
{
    public class SignalPerformanceRepository : ISignalPerformanceRepository
    {
        private readonly StockSenseProDbContext _context;

        public SignalPerformanceRepository(StockSenseProDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(SignalPerformance performance, CancellationToken cancellationToken = default)
        {
            await _context.SignalPerformances.AddAsync(performance, cancellationToken);
        }

        public async Task AddRangeAsync(IEnumerable<SignalPerformance> performances, CancellationToken cancellationToken = default)
        {
            await _context.SignalPerformances.AddRangeAsync(performances, cancellationToken);
        }

        public async Task<IReadOnlyList<SignalPerformance>> GetBySignalIdAsync(Guid tradingSignalId, CancellationToken cancellationToken = default)
        {
            return await _context.SignalPerformances
                .AsNoTracking()
                .Where(p => p.TradingSignalId == tradingSignalId)
                .OrderByDescending(p => p.EvaluatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<SignalPerformance>> GetRecentAsync(string symbol, int take = 20, CancellationToken cancellationToken = default)
        {
            return await _context.SignalPerformances
                .AsNoTracking()
                .Include(p => p.TradingSignal)
                .Where(p => p.TradingSignal.Symbol == symbol)
                .OrderByDescending(p => p.EvaluatedAt)
                .Take(take)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<SignalPerformance>> GetBySymbolAsync(string symbol, CancellationToken cancellationToken = default)
        {
            return await _context.SignalPerformances
                .AsNoTracking()
                .Include(p => p.TradingSignal)
                .Where(p => p.TradingSignal.Symbol == symbol)
                .OrderByDescending(p => p.EvaluatedAt)
                .ToListAsync(cancellationToken);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return _context.SaveChangesAsync(cancellationToken);
        }
    }
}
