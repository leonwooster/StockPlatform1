namespace StockSensePro.Core.ValueObjects
{
    /// <summary>
    /// Value object representing cost metrics for a data provider
    /// </summary>
    public class ProviderCostMetrics
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
