namespace StockSensePro.Core.Configuration;

/// <summary>
/// Configuration settings for caching behavior
/// </summary>
public class CacheSettings
{
    public const string SectionName = "Cache";

    /// <summary>
    /// Default TTL in seconds for cached items (15 minutes)
    /// </summary>
    public int DefaultTTL { get; set; } = 900;

    /// <summary>
    /// TTL in seconds for quote/market data (15 minutes)
    /// </summary>
    public int QuoteTTL { get; set; } = 900;

    /// <summary>
    /// TTL in seconds for historical price data (24 hours)
    /// </summary>
    public int HistoricalTTL { get; set; } = 86400;

    /// <summary>
    /// TTL in seconds for fundamental data (6 hours)
    /// </summary>
    public int FundamentalsTTL { get; set; } = 21600;

    /// <summary>
    /// TTL in seconds for company profile data (7 days)
    /// </summary>
    public int ProfileTTL { get; set; } = 604800;

    /// <summary>
    /// TTL in seconds for search results (1 hour)
    /// </summary>
    public int SearchTTL { get; set; } = 3600;

    /// <summary>
    /// Enable cache warming for frequently requested symbols
    /// </summary>
    public bool EnableCacheWarming { get; set; } = false;

    /// <summary>
    /// Maximum number of items to keep in cache
    /// </summary>
    public int MaxCacheSize { get; set; } = 10000;
}
