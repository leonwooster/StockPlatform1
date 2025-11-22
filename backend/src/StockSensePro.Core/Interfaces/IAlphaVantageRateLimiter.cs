namespace StockSensePro.Core.Interfaces;

/// <summary>
/// Interface for Alpha Vantage rate limiting
/// Enforces API rate limits using token bucket algorithm
/// </summary>
public interface IAlphaVantageRateLimiter
{
    /// <summary>
    /// Attempts to acquire a token for making an API request
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if token was acquired, false if rate limit would be exceeded</returns>
    Task<bool> TryAcquireAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Waits until a token becomes available for making an API request
    /// This method will block until rate limit allows the request
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task WaitForAvailabilityAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current rate limit status
    /// </summary>
    /// <returns>Current rate limit status including remaining requests</returns>
    RateLimitStatus GetStatus();
}

/// <summary>
/// Represents the current status of rate limiting
/// </summary>
public class RateLimitStatus
{
    /// <summary>
    /// Number of requests remaining in the current minute window
    /// </summary>
    public int MinuteRequestsRemaining { get; set; }

    /// <summary>
    /// Maximum requests allowed per minute
    /// </summary>
    public int MinuteRequestsLimit { get; set; }

    /// <summary>
    /// Time until the minute window resets
    /// </summary>
    public TimeSpan MinuteWindowResetIn { get; set; }

    /// <summary>
    /// Number of requests remaining in the current day window
    /// </summary>
    public int DayRequestsRemaining { get; set; }

    /// <summary>
    /// Maximum requests allowed per day
    /// </summary>
    public int DayRequestsLimit { get; set; }

    /// <summary>
    /// Time until the day window resets
    /// </summary>
    public TimeSpan DayWindowResetIn { get; set; }

    /// <summary>
    /// Whether requests are currently being rate limited
    /// </summary>
    public bool IsRateLimited => MinuteRequestsRemaining <= 0 || DayRequestsRemaining <= 0;
}
