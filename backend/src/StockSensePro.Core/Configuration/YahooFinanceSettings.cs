namespace StockSensePro.Core.Configuration;

/// <summary>
/// Configuration settings for Yahoo Finance API integration
/// </summary>
public class YahooFinanceSettings
{
    public const string SectionName = "YahooFinance";

    /// <summary>
    /// Base URL for Yahoo Finance API
    /// </summary>
    public string BaseUrl { get; set; } = "https://query1.finance.yahoo.com";

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
}

/// <summary>
/// Rate limiting configuration for Yahoo Finance API
/// </summary>
public class RateLimitSettings
{
    /// <summary>
    /// Maximum requests allowed per minute
    /// </summary>
    public int RequestsPerMinute { get; set; } = 100;

    /// <summary>
    /// Maximum requests allowed per hour
    /// </summary>
    public int RequestsPerHour { get; set; } = 2000;

    /// <summary>
    /// Maximum requests allowed per day
    /// </summary>
    public int RequestsPerDay { get; set; } = 20000;
}
