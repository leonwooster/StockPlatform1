using System.Linq;
using StockSensePro.Application.Models;
using StockSensePro.Core.Entities;

namespace StockSensePro.API.Models
{
    public class RunBacktestRequest
    {
        public string Symbol { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int HoldingPeriodDays { get; set; } = 5;
        public decimal? StopLossPercent { get; set; }
        public decimal? TakeProfitPercent { get; set; }
        public string Strategy { get; set; } = "default";
    }

    public class BacktestPerformanceResponse
    {
        public BacktestSummary Summary { get; set; } = new();
        public IEnumerable<SignalPerformanceDto> RecentPerformances { get; set; } = Enumerable.Empty<SignalPerformanceDto>();
    }

    public class SignalPerformanceDto
    {
        public Guid Id { get; set; }
        public Guid TradingSignalId { get; set; }
        public DateTime EvaluatedAt { get; set; }
        public decimal ActualReturn { get; set; }
        public decimal BenchmarkReturn { get; set; }
        public bool WasProfitable { get; set; }
        public int DaysHeld { get; set; }
        public decimal? EntryPrice { get; set; }
        public decimal? ExitPrice { get; set; }
        public decimal? MaxDrawdown { get; set; }
        public string Notes { get; set; } = string.Empty;
    }

    public static class BacktestModelMapper
    {
        public static BacktestRequest ToBacktestRequest(this RunBacktestRequest request) => new()
        {
            Symbol = request.Symbol,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            HoldingPeriodDays = request.HoldingPeriodDays,
            StopLossPercent = request.StopLossPercent,
            TakeProfitPercent = request.TakeProfitPercent,
            Strategy = request.Strategy
        };

        public static SignalPerformanceDto ToDto(this SignalPerformance performance) => new()
        {
            Id = performance.Id,
            TradingSignalId = performance.TradingSignalId,
            EvaluatedAt = performance.EvaluatedAt,
            ActualReturn = performance.ActualReturn,
            BenchmarkReturn = performance.BenchmarkReturn,
            WasProfitable = performance.WasProfitable,
            DaysHeld = performance.DaysHeld,
            EntryPrice = performance.EntryPrice,
            ExitPrice = performance.ExitPrice,
            MaxDrawdown = performance.MaxDrawdown,
            Notes = performance.Notes
        };
    }
}
