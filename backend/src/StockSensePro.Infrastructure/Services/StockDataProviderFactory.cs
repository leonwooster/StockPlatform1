using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StockSensePro.Core.Enums;
using StockSensePro.Core.Interfaces;

namespace StockSensePro.Infrastructure.Services
{
    /// <summary>
    /// Factory implementation for creating stock data provider instances.
    /// Uses dependency injection to resolve provider implementations.
    /// Creates a scope to resolve scoped services when called from singleton contexts.
    /// </summary>
    public class StockDataProviderFactory : IStockDataProviderFactory
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<StockDataProviderFactory> _logger;

        public StockDataProviderFactory(
            IServiceScopeFactory scopeFactory,
            ILogger<StockDataProviderFactory> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        /// <summary>
        /// Creates a stock data provider instance based on the specified provider type.
        /// Creates a new scope to resolve scoped services safely from singleton context.
        /// </summary>
        public IStockDataProvider CreateProvider(DataProviderType providerType)
        {
            _logger.LogDebug("Creating provider for type: {ProviderType}", providerType);

            // Create a scope to resolve scoped services
            var scope = _scopeFactory.CreateScope();
            var serviceProvider = scope.ServiceProvider;

            return providerType switch
            {
                DataProviderType.YahooFinance => serviceProvider.GetRequiredService<IYahooFinanceService>(),
                DataProviderType.Mock => serviceProvider.GetRequiredService<MockYahooFinanceService>(),
                DataProviderType.AlphaVantage => serviceProvider.GetRequiredService<AlphaVantageService>(),
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
                    // All providers are now available: YahooFinance, Mock, and AlphaVantage
                    availableProviders.Add(providerType);
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
