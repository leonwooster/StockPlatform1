using Microsoft.Extensions.Logging;
using StockSensePro.Core.Enums;
using StockSensePro.Core.Interfaces;
using StockSensePro.Core.ValueObjects;

namespace StockSensePro.Application.Strategies
{
    /// <summary>
    /// Strategy that selects providers based on cost optimization.
    /// Prefers free providers with available capacity, falling back to paid providers when necessary.
    /// </summary>
    public class CostOptimizedProviderStrategy : ProviderStrategyBase
    {
        // Cost per request for each provider (in USD)
        private static readonly Dictionary<DataProviderType, decimal> ProviderCosts = new()
        {
            { DataProviderType.YahooFinance, 0.0m },      // Free, unlimited
            { DataProviderType.Mock, 0.0m },              // Free, unlimited
            { DataProviderType.AlphaVantage, 0.002m }     // ~$0.002 per request (based on $49.99/month for 500/day premium tier)
        };

        /// <summary>
        /// Initializes a new instance of the CostOptimizedProviderStrategy class
        /// </summary>
        /// <param name="factory">Factory for creating provider instances</param>
        /// <param name="healthMonitor">Health monitor for tracking provider health</param>
        /// <param name="logger">Logger for diagnostic information</param>
        public CostOptimizedProviderStrategy(
            IStockDataProviderFactory factory,
            IProviderHealthMonitor healthMonitor,
            ILogger<CostOptimizedProviderStrategy> logger)
            : base(factory, healthMonitor, logger)
        {
        }

        /// <summary>
        /// Selects the most cost-effective provider that is healthy and has capacity
        /// </summary>
        /// <param name="context">Context information including provider health and rate limits</param>
        /// <returns>The selected data provider instance</returns>
        public override IStockDataProvider SelectProvider(DataProviderContext context)
        {
            // Get all available providers
            var availableProviders = _factory.GetAvailableProviders().ToList();

            if (availableProviders.Count == 0)
            {
                throw new InvalidOperationException("No data providers are available");
            }

            // Filter to healthy providers with rate limit capacity and sort by cost
            var viableProviders = availableProviders
                .Where(p => IsProviderHealthy(context, p) && HasRateLimitCapacity(context, p))
                .OrderBy(p => GetProviderCost(p))
                .ToList();

            // Track excluded providers for monitoring
            var unhealthyProviders = availableProviders
                .Where(p => !IsProviderHealthy(context, p))
                .ToList();
            
            var rateLimitedProviders = availableProviders
                .Where(p => IsProviderHealthy(context, p) && !HasRateLimitCapacity(context, p))
                .ToList();

            // Log excluded providers
            if (unhealthyProviders.Any())
            {
                foreach (var unhealthyProvider in unhealthyProviders)
                {
                    var health = _healthMonitor.GetHealthStatus(unhealthyProvider);
                    _logger.LogWarning(
                        "CostOptimizedProviderStrategy: Excluding unhealthy provider {Provider} (cost: ${Cost:F4}/request, consecutive failures: {Failures})",
                        unhealthyProvider,
                        GetProviderCost(unhealthyProvider),
                        health?.ConsecutiveFailures ?? 0);
                }
            }

            if (rateLimitedProviders.Any())
            {
                _logger.LogDebug(
                    "CostOptimizedProviderStrategy: Excluding rate-limited providers: {Providers}",
                    string.Join(", ", rateLimitedProviders.Select(p => $"{p} (${GetProviderCost(p):F4})")));
            }

            DataProviderType selectedProviderType;

            if (viableProviders.Count > 0)
            {
                // Select the cheapest viable provider
                selectedProviderType = viableProviders.First();
                var health = _healthMonitor.GetHealthStatus(selectedProviderType);
                
                _logger.LogDebug(
                    "CostOptimizedProviderStrategy selected {ProviderType} (cost: ${Cost:F4}/request, avg response: {AvgResponse}ms) for operation {Operation} on symbol {Symbol}",
                    selectedProviderType,
                    GetProviderCost(selectedProviderType),
                    health?.AverageResponseTime.TotalMilliseconds ?? 0,
                    context.Operation,
                    context.Symbol);
            }
            else
            {
                // No viable providers, fall back to cheapest available provider regardless of health/capacity
                selectedProviderType = availableProviders
                    .OrderBy(p => GetProviderCost(p))
                    .First();

                var health = _healthMonitor.GetHealthStatus(selectedProviderType);
                _logger.LogWarning(
                    "CostOptimizedProviderStrategy: No healthy providers with capacity. Falling back to {ProviderType} (cost: ${Cost:F4}/request, consecutive failures: {Failures})",
                    selectedProviderType,
                    GetProviderCost(selectedProviderType),
                    health?.ConsecutiveFailures ?? 0);
            }

            return _factory.CreateProvider(selectedProviderType);
        }

        /// <summary>
        /// Gets the cost per request for a given provider
        /// </summary>
        /// <param name="providerType">The provider type</param>
        /// <returns>Cost per request in USD</returns>
        private static decimal GetProviderCost(DataProviderType providerType)
        {
            return ProviderCosts.TryGetValue(providerType, out var cost) ? cost : decimal.MaxValue;
        }

        /// <summary>
        /// Calculates the estimated cost for a given number of requests
        /// </summary>
        /// <param name="providerType">The provider type</param>
        /// <param name="requestCount">Number of requests</param>
        /// <returns>Estimated cost in USD</returns>
        public static decimal CalculateEstimatedCost(DataProviderType providerType, int requestCount)
        {
            return GetProviderCost(providerType) * requestCount;
        }

        /// <summary>
        /// Gets the strategy name
        /// </summary>
        /// <returns>The strategy name "CostOptimized"</returns>
        public override string GetStrategyName()
        {
            return "CostOptimized";
        }
    }
}
