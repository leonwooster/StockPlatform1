namespace StockSensePro.Infrastructure.Models;

/// <summary>
/// Cache model for calculated fields that are derived from historical data
/// Used to minimize API calls by caching computed values
/// </summary>
public class CalculatedFields
{
    /// <summary>
    /// Stock symbol
    /// </summary>
    public string Symbol { get; set; } = string.Empty;

    /// <summary>
    /// 52-week high price
    /// </summary>
    public decimal? FiftyTwoWeekHigh { get; set; }

    /// <summary>
    /// 52-week low price
    /// </summary>
    public decimal? FiftyTwoWeekLow { get; set; }

    /// <summary>
    /// Average volume over the last 30 days
    /// </summary>
    public long? AverageVolume { get; set; }

    /// <summary>
    /// Timestamp when these fields were calculated
    /// </summary>
    public DateTime CalculatedAt { get; set; }

    /// <summary>
    /// Timestamp when these fields expire (based on cache TTL)
    /// </summary>
    public DateTime ExpiresAt { get; set; }
}
