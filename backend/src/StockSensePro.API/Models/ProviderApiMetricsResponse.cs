namespace StockSensePro.API.Models
{
    /// <summary>
    /// Response model for provider API metrics endpoint
    /// </summary>
    public class ProviderApiMetricsResponse
    {
        /// <summary>
        /// API metrics for each provider
        /// </summary>
        public Dictionary<string, ProviderApiMetrics> Providers { get; set; } = new();

        /// <summary>
        /// The currently active provider
        /// </summary>
        public string? CurrentProvider { get; set; }

        /// <summary>
        /// The current provider selection strategy
        /// </summary>
        public string? Strategy { get; set; }

        /// <summary>
        /// Timestamp when metrics were collected
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// API usage metrics for a single provider
    /// </summary>
    public class ProviderApiMetrics
    {
        /// <summary>
        /// Total number of requests made to this provider
        /// </summary>
        public long TotalRequests { get; set; }

        /// <summary>
        /// Number of successful requests
        /// </summary>
        public long SuccessfulRequests { get; set; }

        /// <summary>
        /// Number of failed requests
        /// </summary>
        public long FailedRequests { get; set; }

        /// <summary>
        /// Success rate as a percentage (0-100)
        /// </summary>
        public double SuccessRate { get; set; }
    }
}
