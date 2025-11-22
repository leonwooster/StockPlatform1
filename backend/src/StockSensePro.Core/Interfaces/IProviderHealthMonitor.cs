using StockSensePro.Core.Enums;
using StockSensePro.Core.ValueObjects;

namespace StockSensePro.Core.Interfaces
{
    /// <summary>
    /// Interface for monitoring the health status of data providers.
    /// Tracks health metrics, response times, and failure rates for each provider.
    /// </summary>
    public interface IProviderHealthMonitor
    {
        /// <summary>
        /// Performs a health check on the specified provider
        /// </summary>
        /// <param name="provider">The provider type to check</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the async operation</returns>
        Task CheckHealthAsync(DataProviderType provider, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the current health status for a specific provider
        /// </summary>
        /// <param name="provider">The provider type</param>
        /// <returns>The current health status, or null if not yet checked</returns>
        ProviderHealth? GetHealthStatus(DataProviderType provider);

        /// <summary>
        /// Gets the health status for all providers
        /// </summary>
        /// <returns>Dictionary of provider health statuses</returns>
        Dictionary<DataProviderType, ProviderHealth> GetAllHealthStatuses();

        /// <summary>
        /// Records a successful request to a provider
        /// </summary>
        /// <param name="provider">The provider type</param>
        /// <param name="responseTime">The response time of the request</param>
        void RecordSuccess(DataProviderType provider, TimeSpan responseTime);

        /// <summary>
        /// Records a failed request to a provider
        /// </summary>
        /// <param name="provider">The provider type</param>
        void RecordFailure(DataProviderType provider);

        /// <summary>
        /// Starts periodic health checks for all configured providers
        /// </summary>
        void StartPeriodicHealthChecks();

        /// <summary>
        /// Stops periodic health checks
        /// </summary>
        void StopPeriodicHealthChecks();
    }
}
