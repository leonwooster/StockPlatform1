using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StockSensePro.Core.Configuration;
using StockSensePro.Core.Interfaces;
using StockSensePro.Core.ValueObjects;

namespace StockSensePro.Application.Strategies
{
    /// <summary>
    /// Strategy that always uses the configured primary provider.
    /// This is the simplest strategy with no fallback or load balancing.
    /// </summary>
    public class PrimaryProviderStrategy : ProviderStrategyBase
    {
        private readonly DataProviderSettings _settings;

        /// <summary>
        /// Initializes a new instance of the PrimaryProviderStrategy class
        /// </summary>
        /// <param name="factory">Factory for creating provider instances</param>
        /// <param name="healthMonitor">Health monitor for tracking provider health</param>
        /// <param name="settings">Data provider configuration settings</param>
        /// <param name="logger">Logger for diagnostic information</param>
        public PrimaryProviderStrategy(
            IStockDataProviderFactory factory,
            IProviderHealthMonitor healthMonitor,
            IOptions<DataProviderSettings> settings,
            ILogger<PrimaryProviderStrategy> logger)
            : base(factory, healthMonitor, logger)
        {
            _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>
        /// Selects the primary provider regardless of context
        /// </summary>
        /// <param name="context">Context information (not used in this strategy)</param>
        /// <returns>The primary data provider instance</returns>
        public override IStockDataProvider SelectProvider(DataProviderContext context)
        {
            var provider = _factory.CreateProvider(_settings.PrimaryProvider);
            
            _logger.LogDebug(
                "PrimaryProviderStrategy selected {ProviderType} for operation {Operation} on symbol {Symbol}",
                _settings.PrimaryProvider,
                context.Operation,
                context.Symbol);

            return provider;
        }

        /// <summary>
        /// Gets the strategy name
        /// </summary>
        /// <returns>The strategy name "Primary"</returns>
        public override string GetStrategyName()
        {
            return "Primary";
        }
    }
}
