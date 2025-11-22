using StockSensePro.Core.Enums;

namespace StockSensePro.Core.Interfaces
{
    /// <summary>
    /// Interface for tracking API call metrics per provider
    /// </summary>
    public interface IProviderMetricsTracker
    {
        /// <summary>
        /// Records a successful API call to a provider
        /// </summary>
        /// <param name="provider">The provider type</param>
        void RecordSuccess(DataProviderType provider);

        /// <summary>
        /// Records a failed API call to a provider
        /// </summary>
        /// <param name="provider">The provider type</param>
        void RecordFailure(DataProviderType provider);

        /// <summary>
        /// Gets the total number of requests for a provider
        /// </summary>
        /// <param name="provider">The provider type</param>
        /// <returns>Total request count</returns>
        long GetTotalRequests(DataProviderType provider);

        /// <summary>
        /// Gets the number of successful requests for a provider
        /// </summary>
        /// <param name="provider">The provider type</param>
        /// <returns>Successful request count</returns>
        long GetSuccessfulRequests(DataProviderType provider);

        /// <summary>
        /// Gets the number of failed requests for a provider
        /// </summary>
        /// <param name="provider">The provider type</param>
        /// <returns>Failed request count</returns>
        long GetFailedRequests(DataProviderType provider);

        /// <summary>
        /// Gets metrics for all providers
        /// </summary>
        /// <returns>Dictionary of provider metrics</returns>
        Dictionary<DataProviderType, (long Total, long Success, long Failed)> GetAllMetrics();

        /// <summary>
        /// Resets metrics for a specific provider
        /// </summary>
        /// <param name="provider">The provider type</param>
        void ResetMetrics(DataProviderType provider);

        /// <summary>
        /// Resets metrics for all providers
        /// </summary>
        void ResetAllMetrics();
    }
}
