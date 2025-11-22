using StockSensePro.Core.Enums;

namespace StockSensePro.Core.Interfaces
{
    /// <summary>
    /// Factory interface for creating stock data provider instances.
    /// Implements the Factory pattern to enable runtime provider selection.
    /// </summary>
    public interface IStockDataProviderFactory
    {
        /// <summary>
        /// Creates a stock data provider instance based on the specified provider type
        /// </summary>
        /// <param name="providerType">The type of provider to create</param>
        /// <returns>An instance of IStockDataProvider</returns>
        /// <exception cref="ArgumentException">Thrown when the provider type is not supported</exception>
        IStockDataProvider CreateProvider(DataProviderType providerType);

        /// <summary>
        /// Creates a stock data provider instance based on the provider name string
        /// </summary>
        /// <param name="providerName">The name of the provider (case-insensitive)</param>
        /// <returns>An instance of IStockDataProvider</returns>
        /// <exception cref="ArgumentException">Thrown when the provider name is not recognized</exception>
        IStockDataProvider CreateProvider(string providerName);

        /// <summary>
        /// Gets a list of all available provider types that can be created
        /// </summary>
        /// <returns>Enumerable of available provider types</returns>
        IEnumerable<DataProviderType> GetAvailableProviders();
    }
}
