namespace StockSensePro.Core.Configuration;

/// <summary>
/// Configuration settings for Alpha Vantage API integration
/// </summary>
public class AlphaVantageSettings
{
    public const string SectionName = "AlphaVantage";

    /// <summary>
    /// API key for Alpha Vantage authentication
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Base URL for Alpha Vantage API
    /// </summary>
    public string BaseUrl { get; set; } = "https://www.alphavantage.co/query";

    /// <summary>
    /// Request timeout in seconds
    /// </summary>
    public int Timeout { get; set; } = 10;

    /// <summary>
    /// Maximum number of retry attempts for failed requests
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Rate limiting configuration
    /// </summary>
    public RateLimitSettings RateLimit { get; set; } = new();

    /// <summary>
    /// Whether Alpha Vantage provider is enabled
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Data enrichment configuration
    /// </summary>
    public DataEnrichmentSettings DataEnrichment { get; set; } = new();
}

/// <summary>
/// Configuration settings for data enrichment features
/// </summary>
public class DataEnrichmentSettings
{
    /// <summary>
    /// Whether to enrich Alpha Vantage data with bid/ask prices from Yahoo Finance
    /// </summary>
    public bool EnableBidAskEnrichment { get; set; } = false;

    /// <summary>
    /// Whether to calculate 52-week high/low from historical data
    /// </summary>
    public bool EnableCalculated52WeekRange { get; set; } = true;

    /// <summary>
    /// Whether to calculate average volume from historical data
    /// </summary>
    public bool EnableCalculatedAverageVolume { get; set; } = true;

    /// <summary>
    /// Cache TTL in seconds for calculated fields (default: 24 hours)
    /// </summary>
    public int CalculatedFieldsCacheTTL { get; set; } = 86400;
}
