using Microsoft.Extensions.Logging;
using StockSensePro.Application.Interfaces;
using StockSensePro.Application.Models;
using StockSensePro.Core.Entities;
using StockSensePro.Core.Interfaces;
using System.Linq;

namespace StockSensePro.Application.Services
{
    public class BacktestService : IBacktestService
    {
        private readonly ITradingSignalRepository _tradingSignalRepository;
        private readonly ISignalPerformanceRepository _signalPerformanceRepository;
        private readonly IHistoricalPriceProvider _historicalPriceProvider;
        private readonly ILogger<BacktestService> _logger;

        public BacktestService(
            ITradingSignalRepository tradingSignalRepository,
            ISignalPerformanceRepository signalPerformanceRepository,
            IHistoricalPriceProvider historicalPriceProvider,
            ILogger<BacktestService> logger)
        {
            _tradingSignalRepository = tradingSignalRepository;
            _signalPerformanceRepository = signalPerformanceRepository;
            _historicalPriceProvider = historicalPriceProvider;
            _logger = logger;
        }

        public async Task<BacktestResult> RunBacktestAsync(BacktestRequest request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.Symbol))
            {
                throw new ArgumentException("Symbol is required", nameof(request.Symbol));
            }

            if (request.StartDate >= request.EndDate)
            {
                throw new ArgumentException("Start date must be earlier than end date");
            }

            var symbol = request.Symbol.ToUpperInvariant();

            // Pull signals within date range
            var signals = await _tradingSignalRepository.GetBySymbolAsync(symbol, cancellationToken);
            var scopedSignals = signals
                .Where(s => s.GeneratedAt >= request.StartDate && s.GeneratedAt <= request.EndDate)
                .OrderBy(s => s.GeneratedAt)
                .ToList();

            var backtestResult = new BacktestResult
            {
                Symbol = symbol,
                StartDate = request.StartDate,
                EndDate = request.EndDate
            };

            if (!scopedSignals.Any())
            {
                _logger.LogInformation("No signals found for symbol {Symbol} between {Start} and {End}", symbol, request.StartDate, request.EndDate);
                return backtestResult;
            }

            // Historical pricing for evaluation period
            var prices = await _historicalPriceProvider.GetHistoricalPricesAsync(symbol, request.StartDate.AddDays(-1), request.EndDate.AddDays(1), cancellationToken);
            var tradeResults = new List<BacktestTradeResult>();
            var performances = new List<SignalPerformance>();

            foreach (var signal in scopedSignals)
            {
                var entryDate = signal.GeneratedAt.Date;
                var holdingPeriod = request.HoldingPeriodDays <= 0 ? 1 : request.HoldingPeriodDays;
                var plannedExitDate = signal.GeneratedAt.AddDays(holdingPeriod).Date;
                if (plannedExitDate > request.EndDate.Date)
                {
                    plannedExitDate = request.EndDate.Date;
                }

                var evaluationDates = prices
                    .Where(p => p.Date.Date >= entryDate && p.Date.Date <= plannedExitDate)
                    .OrderBy(p => p.Date)
                    .ToList();

                if (!evaluationDates.Any())
                {
                    _logger.LogWarning("No evaluation prices found for {Symbol} between {Start} and {End}", symbol, entryDate, plannedExitDate);
                    continue;
                }

                var entryPrice = evaluationDates.First();
                var triggerExitPrice = EvaluateExitPrice(evaluationDates, entryPrice, request);
                var exitData = triggerExitPrice ?? evaluationDates.Last();

                var tradeReturn = entryPrice.Close == 0 ? 0 : (exitData.Close - entryPrice.Close) / entryPrice.Close * 100m;
                var maxDrawdown = CalculateMaxDrawdown(evaluationDates, entryPrice.Close);
                var wasProfitable = tradeReturn >= 0;

                var tradeResult = new BacktestTradeResult
                {
                    SignalId = signal.Id,
                    EntryDate = entryPrice.Date,
                    ExitDate = exitData.Date,
                    EntryPrice = entryPrice.Close,
                    ExitPrice = exitData.Close,
                    Return = Math.Round(tradeReturn, 2),
                    WasProfitable = wasProfitable,
                    SignalType = signal.SignalType,
                    Notes = triggerExitPrice != null ? "Exited via stop/target trigger" : "Exited at end of holding period"
                };

                tradeResults.Add(tradeResult);

                performances.Add(new SignalPerformance
                {
                    Id = Guid.NewGuid(),
                    TradingSignalId = signal.Id,
                    EvaluatedAt = DateTime.UtcNow,
                    ActualReturn = tradeResult.Return,
                    BenchmarkReturn = 0, // TODO: incorporate benchmark when available
                    WasProfitable = wasProfitable,
                    DaysHeld = Math.Max(1, (int)(tradeResult.ExitDate.Date - tradeResult.EntryDate.Date).TotalDays),
                    EntryPrice = tradeResult.EntryPrice,
                    ExitPrice = tradeResult.ExitPrice,
                    MaxDrawdown = Math.Round(maxDrawdown, 2),
                    Notes = tradeResult.Notes
                });
            }

            if (tradeResults.Count == 0)
            {
                return backtestResult;
            }

            backtestResult.Trades = tradeResults.OrderBy(t => t.EntryDate).ToList();
            backtestResult.TotalTrades = tradeResults.Count;
            backtestResult.WinningTrades = tradeResults.Count(t => t.WasProfitable);
            backtestResult.LosingTrades = tradeResults.Count - backtestResult.WinningTrades;
            backtestResult.AverageReturn = Math.Round(tradeResults.Average(t => t.Return), 2);
            backtestResult.CumulativeReturn = Math.Round(tradeResults.Sum(t => t.Return), 2);
            backtestResult.MaxDrawdown = Math.Round(performances.Max(p => p.MaxDrawdown ?? 0), 2);

            await _signalPerformanceRepository.AddRangeAsync(performances, cancellationToken);
            await _signalPerformanceRepository.SaveChangesAsync(cancellationToken);

            return backtestResult;
        }

        public async Task<IReadOnlyList<SignalPerformance>> GetRecentPerformancesAsync(string symbol, int take = 20, CancellationToken cancellationToken = default)
        {
            return await _signalPerformanceRepository.GetRecentAsync(symbol.ToUpperInvariant(), take, cancellationToken);
        }

        public async Task<BacktestSummary> GetPerformanceSummaryAsync(string symbol, CancellationToken cancellationToken = default)
        {
            var normalizedSymbol = symbol.ToUpperInvariant();

            var signals = await _tradingSignalRepository.GetBySymbolAsync(normalizedSymbol, cancellationToken);
            var performances = await _signalPerformanceRepository.GetBySymbolAsync(normalizedSymbol, cancellationToken);

            var summary = new BacktestSummary
            {
                Symbol = normalizedSymbol,
                TotalSignals = signals.Count,
                EvaluatedSignals = performances.Count
            };

            if (performances.Count == 0)
            {
                return summary;
            }

            summary.AverageReturn = Math.Round(performances.Average(p => p.ActualReturn), 2);
            summary.CumulativeReturn = Math.Round(performances.Sum(p => p.ActualReturn), 2);
            summary.WinRate = Math.Round((decimal)performances.Count(p => p.WasProfitable) / performances.Count * 100m, 2);
            summary.MaxDrawdown = Math.Round(performances.Max(p => p.MaxDrawdown ?? 0), 2);
            summary.LastEvaluatedAt = performances.Max(p => p.EvaluatedAt);

            return summary;
        }

        private StockPrice? EvaluateExitPrice(List<StockPrice> evaluationPrices, StockPrice entryPrice, BacktestRequest request)
        {
            if (request.StopLossPercent is null && request.TakeProfitPercent is null)
            {
                return null;
            }

            foreach (var price in evaluationPrices.Skip(1))
            {
                var changePercent = entryPrice.Close == 0 ? 0 : (price.Close - entryPrice.Close) / entryPrice.Close * 100m;

                if (request.TakeProfitPercent.HasValue && changePercent >= request.TakeProfitPercent.Value)
                {
                    return price;
                }

                if (request.StopLossPercent.HasValue && changePercent <= -Math.Abs(request.StopLossPercent.Value))
                {
                    return price;
                }
            }

            return null;
        }

        private decimal CalculateMaxDrawdown(List<StockPrice> evaluationPrices, decimal entryPrice)
        {
            if (!evaluationPrices.Any() || entryPrice == 0)
            {
                return 0;
            }

            var peak = entryPrice;
            var maxDrawdown = 0m;

            foreach (var price in evaluationPrices)
            {
                if (price.Close > peak)
                {
                    peak = price.Close;
                }

                var drawdown = (price.Close - peak) / peak * 100m;
                if (drawdown < maxDrawdown)
                {
                    maxDrawdown = drawdown;
                }
            }

            return Math.Abs(maxDrawdown);
        }
    }
}
