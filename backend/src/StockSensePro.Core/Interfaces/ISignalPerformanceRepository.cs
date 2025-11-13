using StockSensePro.Core.Entities;

namespace StockSensePro.Core.Interfaces
{
    public interface ISignalPerformanceRepository
    {
        Task AddAsync(SignalPerformance performance, CancellationToken cancellationToken = default);
        Task AddRangeAsync(IEnumerable<SignalPerformance> performances, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<SignalPerformance>> GetBySignalIdAsync(Guid tradingSignalId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<SignalPerformance>> GetRecentAsync(string symbol, int take = 20, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<SignalPerformance>> GetBySymbolAsync(string symbol, CancellationToken cancellationToken = default);
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
