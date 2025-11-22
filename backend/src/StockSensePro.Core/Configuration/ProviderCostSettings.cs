namespace StockSensePro.Core.Configuration
{
    /// <summary>
    /// Configuration settings for provider cost tracking and limits
    /// </summary>
    public class ProviderCostSettings
    {
        public const string SectionName = "ProviderCost";

        /// <summary>
        /// Whether cost tracking is enabled
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Whether to enforce cost limits (throttle when exceeded)
        /// </summary>
        public bool EnforceLimits { get; set; } = false;

        /// <summary>
        /// Cost threshold warning percentage (0-100)
        /// </summary>
        public double WarningThresholdPercentage { get; set; } = 80.0;

        /// <summary>
        /// Provider-specific cost configurations
        /// </summary>
        public Dictionary<string, ProviderCostConfig> Providers { get; set; } = new()
        {
            { "YahooFinance", new ProviderCostConfig { CostPerCall = 0.0m, MonthlySubscription = 0.0m, CostThreshold = 0.0m } },
            { "AlphaVantage", new ProviderCostConfig { CostPerCall = 0.002m, MonthlySubscription = 0.0m, CostThreshold = 100.0m } },
            { "Mock", new ProviderCostConfig { CostPerCall = 0.0m, MonthlySubscription = 0.0m, CostThreshold = 0.0m } }
        };
    }

    /// <summary>
    /// Cost configuration for a specific provider
    /// </summary>
    public class ProviderCostConfig
    {
        /// <summary>
        /// Cost per API call in USD
        /// </summary>
        public decimal CostPerCall { get; set; }

        /// <summary>
        /// Monthly subscription cost in USD (0 for free tier)
        /// </summary>
        public decimal MonthlySubscription { get; set; }

        /// <summary>
        /// Cost threshold limit in USD (0 for no limit)
        /// </summary>
        public decimal CostThreshold { get; set; }
    }
}
