using Microsoft.Extensions.Logging;
using StockSensePro.Core.Interfaces;
using StockSensePro.Core.ValueObjects;

namespace StockSensePro.Application.Strategies
{
    /// <summary>
    /// Base abstract class for provider selection strategies.
    /// Provides common functionality for all strategy implementations.
    /// </summary>
    public abstract class ProviderStrategyBase : IDataProviderStrategy
    {
        protected readonly IStockDataProviderFactory _factory;
        protected readonly ILogger _logger;
        protected readonly IProviderHealthMonitor _healthMonitor;

        /// <summary>
        /// Initializes a new instance of the ProviderStrategyBase class
        /// </summary>
        /// <param name="factory">Factory for creating provider instances</param>
        /// <param name="healthMonitor">Health monitor for tracking provider health</param>
        /// <param name="logger">Logger for diagnostic information</param>
        protected ProviderStrategyBase(
            IStockDataProviderFactory factory,
            IProviderHealthMonitor healthMonitor,
            ILogger logger)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _healthMonitor = healthMonitor ?? throw new ArgumentNullException(nameof(healthMonitor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Selects the appropriate data provider based on the current context.
        /// Must be implemented by derived classes.
        /// </summary>
        /// <param name="context">Context information for provider selection</param>
        /// <returns>The selected data provider instance</returns>
        public abstract IStockDataProvider SelectProvider(DataProviderContext context);

        /// <summary>
        /// Gets the fallback provider to use when the primary provider fails.
        /// Can be overridden by derived classes to provide custom fallback logic.
        /// </summary>
        /// <returns>The fallback data provider instance, or null if no fallback is configured</returns>
        public virtual IStockDataProvider? GetFallbackProvider()
        {
            return null;
        }

        /// <summary>
        /// Gets the name of this strategy for logging and monitoring purposes.
        /// Must be implemented by derived classes.
        /// </summary>
        /// <returns>The strategy name</returns>
        public abstract string GetStrategyName();

        /// <summary>
        /// Helper method to log provider selection decisions
        /// </summary>
        /// <param name="context">The context used for selection</param>
        /// <param name="selectedProvider">The provider that was selected</param>
        protected void LogProviderSelection(DataProviderContext context, IStockDataProvider selectedProvider)
        {
            _logger.LogDebug(
                "Strategy {StrategyName} selected provider for operation {Operation} on symbol {Symbol}",
                GetStrategyName(),
                context.Operation,
                context.Symbol);
        }

        /// <summary>
        /// Helper method to check if a provider is healthy based on context
        /// </summary>
        /// <param name="context">The context containing health information</param>
        /// <param name="providerType">The provider type to check</param>
        /// <returns>True if the provider is healthy, false otherwise</returns>
        protected bool IsProviderHealthy(DataProviderContext context, Core.Enums.DataProviderType providerType)
        {
            // First check if context has health information
            if (context.ProviderHealth.ContainsKey(providerType))
            {
                return context.ProviderHealth[providerType].IsHealthy;
            }

            // Fall back to health monitor for real-time status
            var health = _healthMonitor.GetHealthStatus(providerType);
            if (health != null)
            {
                return health.IsHealthy;
            }

            // If no health information is available anywhere, assume healthy
            _logger.LogDebug(
                "No health information available for provider {Provider}, assuming healthy",
                providerType);
            return true;
        }

        /// <summary>
        /// Helper method to check if a provider has available rate limit capacity
        /// </summary>
        /// <param name="context">The context containing rate limit information</param>
        /// <param name="providerType">The provider type to check</param>
        /// <returns>True if the provider has capacity, false otherwise</returns>
        protected bool HasRateLimitCapacity(DataProviderContext context, Core.Enums.DataProviderType providerType)
        {
            if (!context.RateLimitRemaining.ContainsKey(providerType))
            {
                // If no rate limit information is available, assume capacity is available
                return true;
            }

            return context.RateLimitRemaining[providerType] > 0;
        }
    }
}
