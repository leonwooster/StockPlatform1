using StockSensePro.Core.Enums;

namespace StockSensePro.API.Models
{
    /// <summary>
    /// Response model for provider metrics endpoint
    /// </summary>
    public class ProviderMetricsResponse
    {
        /// <summary>
        /// Metrics for each provider
        /// </summary>
        public Dictionary<string, ProviderMetrics> Providers { get; set; } = new();

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
    /// Metrics for a single provider
    /// </summary>
    public class ProviderMetrics
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
        /// Average response time in milliseconds
        /// </summary>
        public double AverageResponseTimeMs { get; set; }

        /// <summary>
        /// Whether the provider is currently healthy
        /// </summary>
        public bool IsHealthy { get; set; }

        /// <summary>
        /// Timestamp of the last health check
        /// </summary>
        public DateTime? LastHealthCheck { get; set; }

        /// <summary>
        /// Number of consecutive failures
        /// </summary>
        public int ConsecutiveFailures { get; set; }

        /// <summary>
        /// Rate limit information (if applicable)
        /// </summary>
        public RateLimitInfo? RateLimit { get; set; }
    }

    /// <summary>
    /// Rate limit information for a provider
    /// </summary>
    public class RateLimitInfo
    {
        /// <summary>
        /// Requests remaining in the current minute window
        /// </summary>
        public int MinuteRequestsRemaining { get; set; }

        /// <summary>
        /// Maximum requests allowed per minute
        /// </summary>
        public int MinuteRequestsLimit { get; set; }

        /// <summary>
        /// Time until the minute window resets
        /// </summary>
        public TimeSpan MinuteWindowResetIn { get; set; }

        /// <summary>
        /// Requests remaining in the current day window
        /// </summary>
        public int DayRequestsRemaining { get; set; }

        /// <summary>
        /// Maximum requests allowed per day
        /// </summary>
        public int DayRequestsLimit { get; set; }

        /// <summary>
        /// Time until the day window resets
        /// </summary>
        public TimeSpan DayWindowResetIn { get; set; }

        /// <summary>
        /// Whether requests are currently being rate limited
        /// </summary>
        public bool IsRateLimited { get; set; }
    }
}
