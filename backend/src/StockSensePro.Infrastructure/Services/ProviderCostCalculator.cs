using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StockSensePro.Core.Configuration;
using StockSensePro.Core.Enums;
using StockSensePro.Core.Interfaces;

namespace StockSensePro.Infrastructure.Services
{
    /// <summary>
    /// Calculates API usage costs per provider based on configured pricing
    /// </summary>
    public class ProviderCostCalculator : IProviderCostCalculator
    {
        private readonly ILogger<ProviderCostCalculator> _logger;
        private readonly ProviderCostSettings _settings;

        /// <summary>
        /// Initializes a new instance of the ProviderCostCalculator
        /// </summary>
        public ProviderCostCalculator(
            ILogger<ProviderCostCalculator> logger,
            IOptions<ProviderCostSettings> settings)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>
        /// Gets the cost per API call for a specific provider
        /// </summary>
        public decimal GetCostPerCall(DataProviderType provider)
        {
            var providerName = provider.ToString();
            
            if (_settings.Providers.TryGetValue(providerName, out var config))
            {
                return config.CostPerCall;
            }

            _logger.LogWarning("No cost configuration found for provider {Provider}, defaulting to 0", provider);
            return 0.0m;
        }

        /// <summary>
        /// Calculates the estimated cost for a given number of API calls
        /// </summary>
        public decimal CalculateCost(DataProviderType provider, long callCount)
        {
            var costPerCall = GetCostPerCall(provider);
            var totalCost = costPerCall * callCount;

            _logger.LogDebug(
                "Calculated cost for {Provider}: {CallCount} calls × ${CostPerCall} = ${TotalCost}",
                provider,
                callCount,
                costPerCall,
                totalCost);

            return totalCost;
        }

        /// <summary>
        /// Gets the monthly subscription cost for a provider (if applicable)
        /// </summary>
        public decimal GetMonthlySubscriptionCost(DataProviderType provider)
        {
            var providerName = provider.ToString();
            
            if (_settings.Providers.TryGetValue(providerName, out var config))
            {
                return config.MonthlySubscription;
            }

            _logger.LogWarning("No subscription cost configuration found for provider {Provider}, defaulting to 0", provider);
            return 0.0m;
        }

        /// <summary>
        /// Gets the total estimated cost including subscription and usage
        /// </summary>
        public decimal GetTotalEstimatedCost(DataProviderType provider, long callCount)
        {
            var usageCost = CalculateCost(provider, callCount);
            var subscriptionCost = GetMonthlySubscriptionCost(provider);
            var totalCost = usageCost + subscriptionCost;

            _logger.LogDebug(
                "Total estimated cost for {Provider}: ${UsageCost} (usage) + ${SubscriptionCost} (subscription) = ${TotalCost}",
                provider,
                usageCost,
                subscriptionCost,
                totalCost);

            return totalCost;
        }
    }
}
