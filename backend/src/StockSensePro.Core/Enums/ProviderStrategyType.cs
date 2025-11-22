namespace StockSensePro.Core.Enums
{
    /// <summary>
    /// Defines the available provider selection strategy types
    /// </summary>
    public enum ProviderStrategyType
    {
        /// <summary>
        /// Use only the primary configured provider
        /// </summary>
        Primary,

        /// <summary>
        /// Use primary provider with automatic fallback to secondary on failure
        /// </summary>
        Fallback,

        /// <summary>
        /// Distribute load evenly across multiple providers using round-robin
        /// </summary>
        RoundRobin,

        /// <summary>
        /// Select provider based on cost optimization and rate limit availability
        /// </summary>
        CostOptimized
    }
}
