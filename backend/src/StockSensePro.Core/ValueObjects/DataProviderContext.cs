using StockSensePro.Core.Enums;

namespace StockSensePro.Core.ValueObjects
{
    /// <summary>
    /// Context information used by provider selection strategies to make decisions.
    /// Contains information about the current operation, provider health, and rate limits.
    /// </summary>
    public class DataProviderContext
    {
        /// <summary>
        /// The stock symbol being requested (e.g., "AAPL")
        /// </summary>
        public string Symbol { get; set; } = string.Empty;

        /// <summary>
        /// The operation being performed (e.g., "GetQuote", "GetHistorical", "GetFundamentals")
        /// </summary>
        public string Operation { get; set; } = string.Empty;

        /// <summary>
        /// Health status for each available provider
        /// </summary>
        public Dictionary<DataProviderType, ProviderHealth> ProviderHealth { get; set; } = new();

        /// <summary>
        /// Remaining rate limit capacity for each provider
        /// </summary>
        public Dictionary<DataProviderType, int> RateLimitRemaining { get; set; } = new();

        /// <summary>
        /// Creates a new instance with empty context
        /// </summary>
        public DataProviderContext()
        {
        }

        /// <summary>
        /// Creates a new instance with specified symbol and operation
        /// </summary>
        public DataProviderContext(string symbol, string operation)
        {
            Symbol = symbol;
            Operation = operation;
        }

        /// <summary>
        /// Creates a new instance with full context information
        /// </summary>
        public DataProviderContext(
            string symbol,
            string operation,
            Dictionary<DataProviderType, ProviderHealth> providerHealth,
            Dictionary<DataProviderType, int> rateLimitRemaining)
        {
            Symbol = symbol;
            Operation = operation;
            ProviderHealth = providerHealth;
            RateLimitRemaining = rateLimitRemaining;
        }
    }
}
