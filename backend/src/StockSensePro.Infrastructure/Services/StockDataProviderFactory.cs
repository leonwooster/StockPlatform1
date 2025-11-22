using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StockSensePro.Core.Enums;
using StockSensePro.Core.Interfaces;

namespace StockSensePro.Infrastructure.Services
{
    /// <summary>
    /// Factory implementation for creating stock data provider instances.
    /// Uses dependency injection to resolve provider implementations.
    /// </summary>
    public class StockDataProviderFactory : IStockDataProviderFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<StockDataProviderFactory> _logger;

        public StockDataProviderFactory(
            IServiceProvider serviceProvider,
            ILogger<StockDataProviderFactory> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        /// <summary>
        /// Creates a stock data provider instance based on the specified provider type
        /// </summary>
        public IStockDataProvider CreateProvider(DataProviderType providerType)
        {
            _logger.LogDebug("Creating provider for type: {ProviderType}", providerType);

            return providerType switch
            {
                DataProviderType.YahooFinance => _serviceProvider.GetRequiredService<IYahooFinanceService>(),
                DataProviderType.Mock => _serviceProvider.GetRequiredService<MockYahooFinanceService>(),
                DataProviderType.AlphaVantage => throw new NotImplementedException(
                    "Alpha Vantage provider is not yet implemented. This will be added in a future task."),
                _ => throw new ArgumentException($"Unsupported provider type: {providerType}", nameof(providerType))
            };
        }

        /// <summary>
        /// Creates a stock data provider instance based on the provider name string
        /// </summary>
        public IStockDataProvider CreateProvider(string providerName)
        {
            if (string.IsNullOrWhiteSpace(providerName))
            {
                throw new ArgumentException("Provider name cannot be null or empty", nameof(providerName));
            }

            if (!Enum.TryParse<DataProviderType>(providerName, ignoreCase: true, out var providerType))
            {
                throw new ArgumentException(
                    $"Unknown provider name: {providerName}. Valid values are: {string.Join(", ", Enum.GetNames<DataProviderType>())}",
                    nameof(providerName));
            }

            return CreateProvider(providerType);
        }

        /// <summary>
        /// Gets a list of all available provider types that can be created
        /// </summary>
        public IEnumerable<DataProviderType> GetAvailableProviders()
        {
            var availableProviders = new List<DataProviderType>();

            // Check which providers are registered and available
            foreach (DataProviderType providerType in Enum.GetValues<DataProviderType>())
            {
                try
                {
                    // For now, we only have YahooFinance and Mock available
                    // AlphaVantage will be added in future tasks
                    if (providerType == DataProviderType.YahooFinance || providerType == DataProviderType.Mock)
                    {
                        availableProviders.Add(providerType);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Provider {ProviderType} is not available", providerType);
                }
            }

            _logger.LogDebug("Available providers: {Providers}", string.Join(", ", availableProviders));
            return availableProviders;
        }
    }
}
