using StockSensePro.Core.ValueObjects;

namespace StockSensePro.Core.Interfaces
{
    /// <summary>
    /// Strategy interface for selecting which data provider to use.
    /// Implements the Strategy pattern to enable different provider selection algorithms.
    /// </summary>
    public interface IDataProviderStrategy
    {
        /// <summary>
        /// Selects the appropriate data provider based on the current context
        /// </summary>
        /// <param name="context">Context information for provider selection</param>
        /// <returns>The selected data provider instance</returns>
        IStockDataProvider SelectProvider(DataProviderContext context);

        /// <summary>
        /// Gets the fallback provider to use when the primary provider fails
        /// </summary>
        /// <returns>The fallback data provider instance, or null if no fallback is configured</returns>
        IStockDataProvider? GetFallbackProvider();

        /// <summary>
        /// Gets the name of this strategy for logging and monitoring purposes
        /// </summary>
        /// <returns>The strategy name</returns>
        string GetStrategyName();
    }
}
