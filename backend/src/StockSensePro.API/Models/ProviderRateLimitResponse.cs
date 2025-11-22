namespace StockSensePro.API.Models
{
    /// <summary>
    /// Response model for provider rate limit status endpoint
    /// </summary>
    public class ProviderRateLimitResponse
    {
        /// <summary>
        /// Rate limit information for each provider
        /// </summary>
        public Dictionary<string, RateLimitInfo> Providers { get; set; } = new();

        /// <summary>
        /// Timestamp when rate limit status was collected
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
