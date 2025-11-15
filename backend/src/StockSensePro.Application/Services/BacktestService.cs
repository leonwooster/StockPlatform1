using Microsoft.Extensions.Logging;
using StockSensePro.Application.Interfaces;
using StockSensePro.Application.Models;
using StockSensePro.Core.Entities;
using StockSensePro.Core.Interfaces;
using System.Linq;
using System.Collections.Concurrent;

namespace StockSensePro.Application.Services
{
    public class BacktestService : IBacktestService
    {
        private readonly ITradingSignalRepository _tradingSignalRepository;
        private readonly ISignalPerformanceRepository _signalPerformanceRepository;
        private readonly IHistoricalPriceProvider _historicalPriceProvider;
        private readonly ILogger<BacktestService> _logger;

        // Simple in-memory cache for equity curve results
        private static readonly ConcurrentDictionary<string, CacheItem> _equityCurveCache = new();
        private static readonly TimeSpan _equityCurveTtl = TimeSpan.FromMinutes(5);

        private sealed class CacheItem
        {
            public DateTime CachedAtUtc { get; init; }
            public IReadOnlyList<EquityCurvePoint> Points { get; init; } = Array.Empty<EquityCurvePoint>();
        }

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

            // Invalidate cached equity curve data for this symbol
            InvalidateEquityCurveCacheForSymbol(symbol);

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

        public async Task<IReadOnlyList<EquityCurvePoint>> GetEquityCurveAsync(string symbol, DateTime? startDate = null, DateTime? endDate = null, bool compounded = true, CancellationToken cancellationToken = default)
        {
            var normalized = symbol.ToUpperInvariant();
            var cacheKey = $"trade:{normalized}:{startDate?.Date:yyyy-MM-dd}:{endDate?.Date:yyyy-MM-dd}:{compounded}";
            if (TryGetEquityCache(cacheKey, out var cached)) return cached;
            var performances = await _signalPerformanceRepository.GetBySymbolAsync(normalized, cancellationToken);

            if (performances is null || performances.Count == 0)
            {
                return Array.Empty<EquityCurvePoint>();
            }

            var filtered = performances.AsEnumerable();
            if (startDate.HasValue)
            {
                var s = startDate.Value.Date;
                filtered = filtered.Where(p => p.EvaluatedAt.Date >= s);
            }
            if (endDate.HasValue)
            {
                var e = endDate.Value.Date;
                filtered = filtered.Where(p => p.EvaluatedAt.Date <= e);
            }

            var ordered = filtered.OrderBy(p => p.EvaluatedAt).ToList();

            var points = new List<EquityCurvePoint>(ordered.Count);
            decimal equity = 100m; // start at index 100
            decimal cumReturn = 0m; // for additive mode

            foreach (var p in ordered)
            {
                if (compounded)
                {
                    var factor = 1m + (p.ActualReturn / 100m);
                    equity = equity * factor;
                }
                else
                {
                    cumReturn += p.ActualReturn;
                    equity = 100m + cumReturn;
                }
                points.Add(new EquityCurvePoint
                {
                    Timestamp = p.EvaluatedAt,
                    Equity = Math.Round(equity, 2)
                });
            }

            SetEquityCache(cacheKey, points);
            return points;
        }

        public async Task<IReadOnlyList<EquityCurvePoint>> GetEquityCurveDailyAsync(string symbol, DateTime startDate, DateTime endDate, bool compounded = true, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(symbol))
            {
                return Array.Empty<EquityCurvePoint>();
            }

            if (startDate.Date > endDate.Date)
            {
                return Array.Empty<EquityCurvePoint>();
            }

            var normalized = symbol.ToUpperInvariant();
            var cacheKey = $"daily:{normalized}:{startDate.Date:yyyy-MM-dd}:{endDate.Date:yyyy-MM-dd}:{compounded}";
            if (TryGetEquityCache(cacheKey, out var cached)) return cached;
            var performances = await _signalPerformanceRepository.GetBySymbolAsync(normalized, cancellationToken);
            if (performances is null || performances.Count == 0)
            {
                return Array.Empty<EquityCurvePoint>();
            }

            var start = startDate.Date;
            var end = endDate.Date;

            var byDay = performances
                .Where(p => p.EvaluatedAt.Date >= start && p.EvaluatedAt.Date <= end)
                .GroupBy(p => p.EvaluatedAt.Date)
                .ToDictionary(g => g.Key, g => g.ToList());

            var result = new List<EquityCurvePoint>();
            decimal equity = 100m;
            decimal cumReturn = 0m;

            for (var d = start; d <= end; d = d.AddDays(1))
            {
                if (byDay.TryGetValue(d, out var dayPerf))
                {
                    if (compounded)
                    {
                        foreach (var p in dayPerf.OrderBy(p => p.EvaluatedAt))
                        {
                            var factor = 1m + (p.ActualReturn / 100m);
                            equity *= factor;
                        }
                    }
                    else
                    {
                        var sum = dayPerf.Sum(p => p.ActualReturn);
                        cumReturn += sum;
                        equity = 100m + cumReturn;
                    }
                }

                result.Add(new EquityCurvePoint
                {
                    Timestamp = d,
                    Equity = Math.Round(equity, 2)
                });
            }

            SetEquityCache(cacheKey, result);
            return result;
        }

        private static bool TryGetEquityCache(string key, out IReadOnlyList<EquityCurvePoint> points)
        {
            points = Array.Empty<EquityCurvePoint>();
            if (_equityCurveCache.TryGetValue(key, out var item))
            {
                if (DateTime.UtcNow - item.CachedAtUtc <= _equityCurveTtl)
                {
                    points = item.Points;
                    return true;
                }
                _equityCurveCache.TryRemove(key, out _);
            }
            return false;
        }

        private static void SetEquityCache(string key, IReadOnlyList<EquityCurvePoint> points)
        {
            _equityCurveCache[key] = new CacheItem
            {
                CachedAtUtc = DateTime.UtcNow,
                Points = points
            };
        }

        private static void InvalidateEquityCurveCacheForSymbol(string symbol)
        {
            var prefixTrade = $"trade:{symbol}:";
            var prefixDaily = $"daily:{symbol}:";
            foreach (var k in _equityCurveCache.Keys)
            {
                if (k.StartsWith(prefixTrade, StringComparison.OrdinalIgnoreCase) || k.StartsWith(prefixDaily, StringComparison.OrdinalIgnoreCase))
                {
                    _equityCurveCache.TryRemove(k, out _);
                }
            }
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
