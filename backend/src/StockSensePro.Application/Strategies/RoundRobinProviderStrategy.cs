using Microsoft.Extensions.Logging;
using StockSensePro.Core.Enums;
using StockSensePro.Core.Interfaces;
using StockSensePro.Core.ValueObjects;

namespace StockSensePro.Application.Strategies
{
    /// <summary>
    /// Strategy that distributes load evenly across multiple providers using round-robin selection.
    /// Only selects from healthy providers with available rate limit capacity.
    /// </summary>
    public class RoundRobinProviderStrategy : ProviderStrategyBase
    {
        private int _currentIndex = 0;
        private readonly object _lock = new object();

        /// <summary>
        /// Initializes a new instance of the RoundRobinProviderStrategy class
        /// </summary>
        /// <param name="factory">Factory for creating provider instances</param>
        /// <param name="healthMonitor">Health monitor for tracking provider health</param>
        /// <param name="logger">Logger for diagnostic information</param>
        public RoundRobinProviderStrategy(
            IStockDataProviderFactory factory,
            IProviderHealthMonitor healthMonitor,
            ILogger<RoundRobinProviderStrategy> logger)
            : base(factory, healthMonitor, logger)
        {
        }

        /// <summary>
        /// Selects the next provider in round-robin fashion from available healthy providers
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

            // Filter to only healthy providers with rate limit capacity
            var healthyProviders = availableProviders
                .Where(p => IsProviderHealthy(context, p) && HasRateLimitCapacity(context, p))
                .ToList();

            // Track which providers were excluded and why
            var unhealthyProviders = availableProviders
                .Where(p => !IsProviderHealthy(context, p))
                .ToList();
            
            var rateLimitedProviders = availableProviders
                .Where(p => IsProviderHealthy(context, p) && !HasRateLimitCapacity(context, p))
                .ToList();

            // Log excluded providers for monitoring
            if (unhealthyProviders.Any())
            {
                foreach (var unhealthyProvider in unhealthyProviders)
                {
                    var health = _healthMonitor.GetHealthStatus(unhealthyProvider);
                    _logger.LogWarning(
                        "RoundRobinProviderStrategy: Excluding unhealthy provider {Provider} (consecutive failures: {Failures}, last checked: {LastChecked})",
                        unhealthyProvider,
                        health?.ConsecutiveFailures ?? 0,
                        health?.LastChecked ?? DateTime.MinValue);
                }
            }

            if (rateLimitedProviders.Any())
            {
                _logger.LogDebug(
                    "RoundRobinProviderStrategy: Excluding rate-limited providers: {Providers}",
                    string.Join(", ", rateLimitedProviders));
            }

            // If no healthy providers, use all available providers as fallback
            if (healthyProviders.Count == 0)
            {
                _logger.LogWarning(
                    "RoundRobinProviderStrategy: No healthy providers with capacity available. Using all providers as fallback.");
                healthyProviders = availableProviders;
            }

            // Thread-safe round-robin selection
            DataProviderType selectedProviderType;
            lock (_lock)
            {
                selectedProviderType = healthyProviders[_currentIndex % healthyProviders.Count];
                _currentIndex++;
                
                // Prevent overflow by resetting when we've cycled through all providers
                if (_currentIndex >= healthyProviders.Count * 1000)
                {
                    _currentIndex = 0;
                }
            }

            var provider = _factory.CreateProvider(selectedProviderType);
            var selectedHealth = _healthMonitor.GetHealthStatus(selectedProviderType);

            _logger.LogDebug(
                "RoundRobinProviderStrategy selected {ProviderType} (index {Index} of {Count} healthy providers, avg response: {AvgResponse}ms) for operation {Operation} on symbol {Symbol}",
                selectedProviderType,
                _currentIndex - 1,
                healthyProviders.Count,
                selectedHealth?.AverageResponseTime.TotalMilliseconds ?? 0,
                context.Operation,
                context.Symbol);

            return provider;
        }

        /// <summary>
        /// Gets the strategy name
        /// </summary>
        /// <returns>The strategy name "RoundRobin"</returns>
        public override string GetStrategyName()
        {
            return "RoundRobin";
        }
    }
}
