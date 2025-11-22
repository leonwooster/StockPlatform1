using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StockSensePro.Core.Configuration;
using StockSensePro.Core.Enums;
using StockSensePro.Core.Interfaces;
using StockSensePro.Core.ValueObjects;
using System.Collections.Concurrent;

namespace StockSensePro.Infrastructure.Services
{
    /// <summary>
    /// Tracks API usage costs per provider and monitors cost thresholds
    /// </summary>
    public class ProviderCostTracker : IProviderCostTracker
    {
        private readonly IProviderCostCalculator _costCalculator;
        private readonly ILogger<ProviderCostTracker> _logger;
        private readonly ProviderCostSettings _settings;
        private readonly ConcurrentDictionary<DataProviderType, long> _apiCallCounts;
        private readonly ConcurrentDictionary<DataProviderType, DateTime> _trackingStartTimes;
        private readonly object _lock = new();

        /// <summary>
        /// Initializes a new instance of the ProviderCostTracker
        /// </summary>
        public ProviderCostTracker(
            IProviderCostCalculator costCalculator,
            ILogger<ProviderCostTracker> logger,
            IOptions<ProviderCostSettings> settings)
        {
            _costCalculator = costCalculator ?? throw new ArgumentNullException(nameof(costCalculator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
            
            _apiCallCounts = new ConcurrentDictionary<DataProviderType, long>();
            _trackingStartTimes = new ConcurrentDictionary<DataProviderType, DateTime>();

            // Initialize tracking for all provider types
            InitializeTracking();
        }

        /// <summary>
        /// Initializes tracking for all provider types
        /// </summary>
        private void InitializeTracking()
        {
            foreach (DataProviderType provider in Enum.GetValues(typeof(DataProviderType)))
            {
                _apiCallCounts.TryAdd(provider, 0);
                _trackingStartTimes.TryAdd(provider, DateTime.UtcNow);
            }

            _logger.LogInformation("Initialized cost tracking for {Count} providers", _apiCallCounts.Count);
        }

        /// <summary>
        /// Records an API call for cost tracking
        /// </summary>
        public void RecordApiCall(DataProviderType provider)
        {
            if (!_settings.Enabled)
            {
                return;
            }

            var newCount = _apiCallCounts.AddOrUpdate(provider, 1, (key, oldValue) => oldValue + 1);

            _logger.LogDebug("Recorded API call for {Provider} - Total calls: {Count}", provider, newCount);

            // Check if threshold is exceeded and log warning
            if (IsCostThresholdExceeded(provider))
            {
                var metrics = GetCostMetrics(provider);
                _logger.LogWarning(
                    "Cost threshold exceeded for {Provider}: ${CurrentCost} / ${Threshold} ({Percentage:F1}%)",
                    provider,
                    metrics.TotalEstimatedCost,
                    metrics.CostThreshold,
                    metrics.ThresholdPercentage);
            }
            else
            {
                var percentage = GetCostThresholdPercentage(provider);
                if (percentage >= _settings.WarningThresholdPercentage)
                {
                    var metrics = GetCostMetrics(provider);
                    _logger.LogWarning(
                        "Cost threshold warning for {Provider}: ${CurrentCost} / ${Threshold} ({Percentage:F1}%)",
                        provider,
                        metrics.TotalEstimatedCost,
                        metrics.CostThreshold,
                        metrics.ThresholdPercentage);
                }
            }
        }

        /// <summary>
        /// Gets the current cost metrics for a specific provider
        /// </summary>
        public ProviderCostMetrics GetCostMetrics(DataProviderType provider)
        {
            var callCount = _apiCallCounts.GetOrAdd(provider, 0);
            var costPerCall = _costCalculator.GetCostPerCall(provider);
            var estimatedCost = _costCalculator.CalculateCost(provider, callCount);
            var subscriptionCost = _costCalculator.GetMonthlySubscriptionCost(provider);
            var totalCost = _costCalculator.GetTotalEstimatedCost(provider, callCount);
            var threshold = GetCostThreshold(provider);
            var thresholdPercentage = threshold > 0 ? (double)(totalCost / threshold) * 100.0 : 0.0;
            var isThresholdExceeded = threshold > 0 && totalCost >= threshold;

            var trackingStarted = _trackingStartTimes.GetOrAdd(provider, DateTime.UtcNow);

            return new ProviderCostMetrics
            {
                TotalApiCalls = callCount,
                EstimatedCost = estimatedCost,
                MonthlySubscriptionCost = subscriptionCost,
                TotalEstimatedCost = totalCost,
                CostPerCall = costPerCall,
                CostThreshold = threshold,
                ThresholdPercentage = thresholdPercentage,
                IsThresholdExceeded = isThresholdExceeded,
                TrackingStarted = trackingStarted,
                LastUpdated = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Gets cost metrics for all providers
        /// </summary>
        public Dictionary<DataProviderType, ProviderCostMetrics> GetAllCostMetrics()
        {
            var allMetrics = new Dictionary<DataProviderType, ProviderCostMetrics>();

            foreach (DataProviderType provider in Enum.GetValues(typeof(DataProviderType)))
            {
                allMetrics[provider] = GetCostMetrics(provider);
            }

            return allMetrics;
        }

        /// <summary>
        /// Checks if a provider has exceeded its cost threshold
        /// </summary>
        public bool IsCostThresholdExceeded(DataProviderType provider)
        {
            if (!_settings.Enabled)
            {
                return false;
            }

            var threshold = GetCostThreshold(provider);
            if (threshold <= 0)
            {
                return false; // No threshold configured
            }

            var callCount = _apiCallCounts.GetOrAdd(provider, 0);
            var totalCost = _costCalculator.GetTotalEstimatedCost(provider, callCount);

            return totalCost >= threshold;
        }

        /// <summary>
        /// Gets the percentage of cost threshold used
        /// </summary>
        public double GetCostThresholdPercentage(DataProviderType provider)
        {
            var threshold = GetCostThreshold(provider);
            if (threshold <= 0)
            {
                return 0.0; // No threshold configured
            }

            var callCount = _apiCallCounts.GetOrAdd(provider, 0);
            var totalCost = _costCalculator.GetTotalEstimatedCost(provider, callCount);

            return (double)(totalCost / threshold) * 100.0;
        }

        /// <summary>
        /// Gets the cost threshold for a provider
        /// </summary>
        private decimal GetCostThreshold(DataProviderType provider)
        {
            var providerName = provider.ToString();
            
            if (_settings.Providers.TryGetValue(providerName, out var config))
            {
                return config.CostThreshold;
            }

            return 0.0m; // No threshold
        }

        /// <summary>
        /// Resets cost tracking for a specific provider
        /// </summary>
        public void ResetCostTracking(DataProviderType provider)
        {
            lock (_lock)
            {
                _apiCallCounts[provider] = 0;
                _trackingStartTimes[provider] = DateTime.UtcNow;
            }

            _logger.LogInformation("Reset cost tracking for provider: {Provider}", provider);
        }

        /// <summary>
        /// Resets cost tracking for all providers
        /// </summary>
        public void ResetAllCostTracking()
        {
            lock (_lock)
            {
                foreach (DataProviderType provider in Enum.GetValues(typeof(DataProviderType)))
                {
                    _apiCallCounts[provider] = 0;
                    _trackingStartTimes[provider] = DateTime.UtcNow;
                }
            }

            _logger.LogInformation("Reset cost tracking for all providers");
        }
    }
}
