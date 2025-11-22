namespace StockSensePro.API.Models
{
    /// <summary>
    /// Response model for provider health status endpoint
    /// </summary>
    public class ProviderHealthResponse
    {
        /// <summary>
        /// Health status for each provider
        /// </summary>
        public Dictionary<string, ProviderHealthStatus> Providers { get; set; } = new();

        /// <summary>
        /// Whether at least one provider is healthy
        /// </summary>
        public bool OverallHealthy { get; set; }

        /// <summary>
        /// Timestamp when health status was collected
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Health status for a single provider
    /// </summary>
    public class ProviderHealthStatus
    {
        /// <summary>
        /// Whether the provider is currently healthy
        /// </summary>
        public bool IsHealthy { get; set; }

        /// <summary>
        /// Timestamp of the last health check
        /// </summary>
        public DateTime LastChecked { get; set; }

        /// <summary>
        /// Number of consecutive failures
        /// </summary>
        public int ConsecutiveFailures { get; set; }

        /// <summary>
        /// Average response time in milliseconds
        /// </summary>
        public double AverageResponseTimeMs { get; set; }
    }
}
