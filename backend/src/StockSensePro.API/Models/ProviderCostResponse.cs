namespace StockSensePro.API.Models
{
    /// <summary>
    /// Response model for provider cost metrics endpoint
    /// </summary>
    public class ProviderCostResponse
    {
        /// <summary>
        /// Timestamp when the metrics were retrieved
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Cost metrics per provider
        /// </summary>
        public Dictionary<string, ProviderCostInfo> Providers { get; set; } = new();

        /// <summary>
        /// Total estimated cost across all providers
        /// </summary>
        public decimal TotalEstimatedCost { get; set; }

        /// <summary>
        /// Total API calls across all providers
        /// </summary>
        public long TotalApiCalls { get; set; }

        /// <summary>
        /// Whether cost tracking is enabled
        /// </summary>
        public bool CostTrackingEnabled { get; set; }

        /// <summary>
        /// Whether cost limits are enforced
        /// </summary>
        public bool CostLimitsEnforced { get; set; }
    }

    /// <summary>
    /// Cost information for a specific provider
    /// </summary>
    public class ProviderCostInfo
    {
        /// <summary>
        /// Total number of API calls made
        /// </summary>
        public long TotalApiCalls { get; set; }

        /// <summary>
        /// Estimated cost based on API calls (in USD)
        /// </summary>
        public decimal EstimatedCost { get; set; }

        /// <summary>
        /// Monthly subscription cost (in USD)
        /// </summary>
        public decimal MonthlySubscriptionCost { get; set; }

        /// <summary>
        /// Total estimated cost including subscription (in USD)
        /// </summary>
        public decimal TotalEstimatedCost { get; set; }

        /// <summary>
        /// Cost per API call (in USD)
        /// </summary>
        public decimal CostPerCall { get; set; }

        /// <summary>
        /// Cost threshold limit (in USD)
        /// </summary>
        public decimal CostThreshold { get; set; }

        /// <summary>
        /// Percentage of cost threshold used
        /// </summary>
        public double ThresholdPercentage { get; set; }

        /// <summary>
        /// Whether the cost threshold has been exceeded
        /// </summary>
        public bool IsThresholdExceeded { get; set; }

        /// <summary>
        /// Timestamp when tracking started
        /// </summary>
        public DateTime TrackingStarted { get; set; }

        /// <summary>
        /// Timestamp of last update
        /// </summary>
        public DateTime LastUpdated { get; set; }
    }
}
