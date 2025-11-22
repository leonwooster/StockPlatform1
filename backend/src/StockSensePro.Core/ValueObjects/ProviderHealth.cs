namespace StockSensePro.Core.ValueObjects
{
    /// <summary>
    /// Represents the health status of a data provider.
    /// Used for monitoring and provider selection decisions.
    /// </summary>
    public class ProviderHealth
    {
        /// <summary>
        /// Indicates whether the provider is currently healthy and available
        /// </summary>
        public bool IsHealthy { get; set; }

        /// <summary>
        /// The timestamp of the last health check
        /// </summary>
        public DateTime LastChecked { get; set; }

        /// <summary>
        /// The number of consecutive failures since the last successful request
        /// </summary>
        public int ConsecutiveFailures { get; set; }

        /// <summary>
        /// The average response time for requests to this provider
        /// </summary>
        public TimeSpan AverageResponseTime { get; set; }

        /// <summary>
        /// Creates a new instance with default healthy state
        /// </summary>
        public ProviderHealth()
        {
            IsHealthy = true;
            LastChecked = DateTime.UtcNow;
            ConsecutiveFailures = 0;
            AverageResponseTime = TimeSpan.Zero;
        }

        /// <summary>
        /// Creates a new instance with specified values
        /// </summary>
        public ProviderHealth(bool isHealthy, DateTime lastChecked, int consecutiveFailures, TimeSpan averageResponseTime)
        {
            IsHealthy = isHealthy;
            LastChecked = lastChecked;
            ConsecutiveFailures = consecutiveFailures;
            AverageResponseTime = averageResponseTime;
        }
    }
}
