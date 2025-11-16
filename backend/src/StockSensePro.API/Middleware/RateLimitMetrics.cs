using System.Collections.Concurrent;

namespace StockSensePro.API.Middleware;

/// <summary>
/// Tracks rate limiting metrics for monitoring and observability
/// </summary>
public class RateLimitMetrics
{
    private readonly ConcurrentDictionary<string, long> _requestCounts;
    private readonly ConcurrentDictionary<string, long> _rateLimitHits;
    private readonly DateTime _startTime;

    public RateLimitMetrics()
    {
        _requestCounts = new ConcurrentDictionary<string, long>();
        _rateLimitHits = new ConcurrentDictionary<string, long>();
        _startTime = DateTime.UtcNow;
    }

    /// <summary>
    /// Increments the request count for an endpoint
    /// </summary>
    public void IncrementRequestCount(string endpoint)
    {
        _requestCounts.AddOrUpdate(endpoint, 1, (_, count) => count + 1);
    }

    /// <summary>
    /// Increments the rate limit hit count for an endpoint
    /// </summary>
    public void IncrementRateLimitHits(string endpoint)
    {
        _rateLimitHits.AddOrUpdate(endpoint, 1, (_, count) => count + 1);
    }

    /// <summary>
    /// Gets the total request count for an endpoint
    /// </summary>
    public long GetRequestCount(string endpoint)
    {
        return _requestCounts.TryGetValue(endpoint, out var count) ? count : 0;
    }

    /// <summary>
    /// Gets the total rate limit hits for an endpoint
    /// </summary>
    public long GetRateLimitHits(string endpoint)
    {
        return _rateLimitHits.TryGetValue(endpoint, out var count) ? count : 0;
    }

    /// <summary>
    /// Gets all request counts by endpoint
    /// </summary>
    public Dictionary<string, long> GetAllRequestCounts()
    {
        return new Dictionary<string, long>(_requestCounts);
    }

    /// <summary>
    /// Gets all rate limit hits by endpoint
    /// </summary>
    public Dictionary<string, long> GetAllRateLimitHits()
    {
        return new Dictionary<string, long>(_rateLimitHits);
    }

    /// <summary>
    /// Gets the uptime since metrics started tracking
    /// </summary>
    public TimeSpan GetUptime()
    {
        return DateTime.UtcNow - _startTime;
    }

    /// <summary>
    /// Gets a summary of all metrics
    /// </summary>
    public RateLimitMetricsSummary GetSummary()
    {
        return new RateLimitMetricsSummary
        {
            TotalRequests = _requestCounts.Values.Sum(),
            TotalRateLimitHits = _rateLimitHits.Values.Sum(),
            RequestsByEndpoint = GetAllRequestCounts(),
            RateLimitHitsByEndpoint = GetAllRateLimitHits(),
            Uptime = GetUptime()
        };
    }
}

/// <summary>
/// Summary of rate limiting metrics
/// </summary>
public class RateLimitMetricsSummary
{
    public long TotalRequests { get; set; }
    public long TotalRateLimitHits { get; set; }
    public Dictionary<string, long> RequestsByEndpoint { get; set; } = new();
    public Dictionary<string, long> RateLimitHitsByEndpoint { get; set; } = new();
    public TimeSpan Uptime { get; set; }
    public double RateLimitHitRate => TotalRequests > 0 ? (double)TotalRateLimitHits / TotalRequests : 0;
}
