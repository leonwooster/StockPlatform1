using StockSensePro.Core.Entities;

namespace StockSensePro.Core.Interfaces
{
    public interface ITradingSignalRepository
    {
        Task AddAsync(TradingSignal signal, CancellationToken cancellationToken = default);
        Task AddRangeAsync(IEnumerable<TradingSignal> signals, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<TradingSignal>> GetBySymbolAsync(string symbol, CancellationToken cancellationToken = default);
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
