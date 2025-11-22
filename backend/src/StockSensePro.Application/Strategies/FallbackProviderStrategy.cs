using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StockSensePro.Core.Configuration;
using StockSensePro.Core.Interfaces;
using StockSensePro.Core.ValueObjects;

namespace StockSensePro.Application.Strategies
{
    /// <summary>
    /// Strategy that uses a primary provider with automatic fallback to a secondary provider.
    /// Falls back when the primary provider is unhealthy or fails.
    /// </summary>
    public class FallbackProviderStrategy : ProviderStrategyBase
    {
        private readonly DataProviderSettings _settings;

        /// <summary>
        /// Initializes a new instance of the FallbackProviderStrategy class
        /// </summary>
        /// <param name="factory">Factory for creating provider instances</param>
        /// <param name="healthMonitor">Health monitor for tracking provider health</param>
        /// <param name="settings">Data provider configuration settings</param>
        /// <param name="logger">Logger for diagnostic information</param>
        public FallbackProviderStrategy(
            IStockDataProviderFactory factory,
            IProviderHealthMonitor healthMonitor,
            IOptions<DataProviderSettings> settings,
            ILogger<FallbackProviderStrategy> logger)
            : base(factory, healthMonitor, logger)
        {
            _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>
        /// Selects the primary provider if healthy, otherwise falls back to secondary
        /// </summary>
        /// <param name="context">Context information including provider health status</param>
        /// <returns>The selected data provider instance</returns>
        public override IStockDataProvider SelectProvider(DataProviderContext context)
        {
            // Get real-time health status from health monitor
            var primaryHealth = _healthMonitor.GetHealthStatus(_settings.PrimaryProvider);
            var isPrimaryHealthy = IsProviderHealthy(context, _settings.PrimaryProvider);

            // Check if primary provider is healthy
            if (isPrimaryHealthy)
            {
                var primaryProvider = _factory.CreateProvider(_settings.PrimaryProvider);
                
                // Log recovery if primary was previously unhealthy
                if (primaryHealth != null && primaryHealth.ConsecutiveFailures > 0)
                {
                    _logger.LogInformation(
                        "FallbackProviderStrategy: Primary provider {ProviderType} has recovered after {Failures} consecutive failures. Resuming normal operation.",
                        _settings.PrimaryProvider,
                        primaryHealth.ConsecutiveFailures);
                }
                
                _logger.LogDebug(
                    "FallbackProviderStrategy selected primary provider {ProviderType} for operation {Operation} on symbol {Symbol}",
                    _settings.PrimaryProvider,
                    context.Operation,
                    context.Symbol);

                return primaryProvider;
            }

            // Primary is unhealthy, try fallback
            if (_settings.FallbackProvider.HasValue)
            {
                var fallbackHealth = _healthMonitor.GetHealthStatus(_settings.FallbackProvider.Value);
                var isFallbackHealthy = IsProviderHealthy(context, _settings.FallbackProvider.Value);

                if (isFallbackHealthy)
                {
                    var fallbackProvider = _factory.CreateProvider(_settings.FallbackProvider.Value);
                    
                    _logger.LogWarning(
                        "FallbackProviderStrategy falling back to {FallbackProvider} because primary provider {PrimaryProvider} is unhealthy (consecutive failures: {Failures}) for operation {Operation} on symbol {Symbol}",
                        _settings.FallbackProvider.Value,
                        _settings.PrimaryProvider,
                        primaryHealth?.ConsecutiveFailures ?? 0,
                        context.Operation,
                        context.Symbol);

                    return fallbackProvider;
                }
                else
                {
                    _logger.LogError(
                        "FallbackProviderStrategy: Both primary {PrimaryProvider} (failures: {PrimaryFailures}) and fallback {FallbackProvider} (failures: {FallbackFailures}) providers are unhealthy. Using primary anyway.",
                        _settings.PrimaryProvider,
                        primaryHealth?.ConsecutiveFailures ?? 0,
                        _settings.FallbackProvider.Value,
                        fallbackHealth?.ConsecutiveFailures ?? 0);
                }
            }
            else
            {
                _logger.LogWarning(
                    "FallbackProviderStrategy: Primary provider {PrimaryProvider} is unhealthy (consecutive failures: {Failures}) but no fallback is configured. Using primary anyway.",
                    _settings.PrimaryProvider,
                    primaryHealth?.ConsecutiveFailures ?? 0);
            }

            // No fallback configured or fallback is also unhealthy, use primary anyway
            return _factory.CreateProvider(_settings.PrimaryProvider);
        }

        /// <summary>
        /// Gets the fallback provider if configured
        /// </summary>
        /// <returns>The fallback data provider instance, or null if not configured</returns>
        public override IStockDataProvider? GetFallbackProvider()
        {
            if (_settings.FallbackProvider.HasValue)
            {
                return _factory.CreateProvider(_settings.FallbackProvider.Value);
            }

            return null;
        }

        /// <summary>
        /// Gets the strategy name
        /// </summary>
        /// <returns>The strategy name "Fallback"</returns>
        public override string GetStrategyName()
        {
            return "Fallback";
        }
    }
}
