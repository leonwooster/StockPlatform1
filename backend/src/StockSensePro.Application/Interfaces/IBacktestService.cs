using StockSensePro.Application.Models;
using StockSensePro.Core.Entities;

namespace StockSensePro.Application.Interfaces
{
    public interface IBacktestService
    {
        Task<BacktestResult> RunBacktestAsync(BacktestRequest request, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<SignalPerformance>> GetRecentPerformancesAsync(string symbol, int take = 20, CancellationToken cancellationToken = default);
        Task<BacktestSummary> GetPerformanceSummaryAsync(string symbol, CancellationToken cancellationToken = default);
    }
}
