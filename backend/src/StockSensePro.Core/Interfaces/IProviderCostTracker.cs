using StockSensePro.Core.Enums;
using StockSensePro.Core.ValueObjects;

namespace StockSensePro.Core.Interfaces
{
    /// <summary>
    /// Interface for tracking API usage costs per provider
    /// </summary>
    public interface IProviderCostTracker
    {
        /// <summary>
        /// Records an API call for cost tracking
        /// </summary>
        /// <param name="provider">The provider type</param>
        void RecordApiCall(DataProviderType provider);

        /// <summary>
        /// Gets the current cost metrics for a specific provider
        /// </summary>
        /// <param name="provider">The provider type</param>
        /// <returns>Cost metrics for the provider</returns>
        ProviderCostMetrics GetCostMetrics(DataProviderType provider);

        /// <summary>
        /// Gets cost metrics for all providers
        /// </summary>
        /// <returns>Dictionary of provider cost metrics</returns>
        Dictionary<DataProviderType, ProviderCostMetrics> GetAllCostMetrics();

        /// <summary>
        /// Checks if a provider has exceeded its cost threshold
        /// </summary>
        /// <param name="provider">The provider type</param>
        /// <returns>True if cost threshold is exceeded</returns>
        bool IsCostThresholdExceeded(DataProviderType provider);

        /// <summary>
        /// Gets the percentage of cost threshold used
        /// </summary>
        /// <param name="provider">The provider type</param>
        /// <returns>Percentage of threshold used (0-100+)</returns>
        double GetCostThresholdPercentage(DataProviderType provider);

        /// <summary>
        /// Resets cost tracking for a specific provider
        /// </summary>
        /// <param name="provider">The provider type</param>
        void ResetCostTracking(DataProviderType provider);

        /// <summary>
        /// Resets cost tracking for all providers
        /// </summary>
        void ResetAllCostTracking();
    }
}
