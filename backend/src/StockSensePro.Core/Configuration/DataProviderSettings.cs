using StockSensePro.Core.Enums;

namespace StockSensePro.Core.Configuration
{
    /// <summary>
    /// Configuration settings for data provider selection and behavior
    /// </summary>
    public class DataProviderSettings
    {
        /// <summary>
        /// Configuration section name in appsettings.json
        /// </summary>
        public const string SectionName = "DataProvider";

        /// <summary>
        /// The primary data provider to use
        /// </summary>
        public DataProviderType PrimaryProvider { get; set; } = DataProviderType.YahooFinance;

        /// <summary>
        /// The fallback provider to use when primary fails (optional)
        /// </summary>
        public DataProviderType? FallbackProvider { get; set; }

        /// <summary>
        /// The strategy to use for provider selection
        /// </summary>
        public ProviderStrategyType Strategy { get; set; } = ProviderStrategyType.Primary;

        /// <summary>
        /// Whether to automatically fallback to secondary provider on failure
        /// </summary>
        public bool EnableAutomaticFallback { get; set; } = true;

        /// <summary>
        /// Interval in seconds between provider health checks
        /// </summary>
        public int HealthCheckIntervalSeconds { get; set; } = 60;
    }
}
