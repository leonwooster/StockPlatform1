using StockSensePro.Application.Models;
using StockSensePro.Core.Entities;

namespace StockSensePro.Application.Interfaces
{
    public interface IBacktestService
    {
        Task<BacktestResult> RunBacktestAsync(BacktestRequest request, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<SignalPerformance>> GetRecentPerformancesAsync(string symbol, int take = 20, CancellationToken cancellationToken = default);
        Task<BacktestSummary> GetPerformanceSummaryAsync(string symbol, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<EquityCurvePoint>> GetEquityCurveAsync(string symbol, DateTime? startDate = null, DateTime? endDate = null, bool compounded = true, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<EquityCurvePoint>> GetEquityCurveDailyAsync(string symbol, DateTime startDate, DateTime endDate, bool compounded = true, CancellationToken cancellationToken = default);
    }
}
