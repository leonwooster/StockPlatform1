using System.Collections.Concurrent;
using System.Net;
using Microsoft.Extensions.Options;
using StockSensePro.Core.Configuration;

namespace StockSensePro.API.Middleware;

/// <summary>
/// Middleware for rate limiting API requests using token bucket algorithm
/// </summary>
public class RateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitMiddleware> _logger;
    private readonly RateLimitSettings _rateLimitSettings;
    private readonly ConcurrentDictionary<string, TokenBucket> _buckets;
    private readonly RateLimitMetrics _metrics;

    public RateLimitMiddleware(
        RequestDelegate next,
        ILogger<RateLimitMiddleware> logger,
        YahooFinanceSettings yahooFinanceSettings)
    {
        _next = next;
        _logger = logger;
        _rateLimitSettings = yahooFinanceSettings.RateLimit;
        _buckets = new ConcurrentDictionary<string, TokenBucket>();
        _metrics = new RateLimitMetrics();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only apply rate limiting to Yahoo Finance related endpoints
        var path = context.Request.Path.Value?.ToLower() ?? string.Empty;
        
        if (!ShouldApplyRateLimit(path))
        {
            await _next(context);
            return;
        }

        var endpoint = GetEndpointKey(path);
        
        // Check rate limits for different time windows
        var minuteBucket = GetOrCreateBucket($"{endpoint}:minute", TimeSpan.FromMinutes(1), _rateLimitSettings.RequestsPerMinute);
        var hourBucket = GetOrCreateBucket($"{endpoint}:hour", TimeSpan.FromHours(1), _rateLimitSettings.RequestsPerHour);
        var dayBucket = GetOrCreateBucket($"{endpoint}:day", TimeSpan.FromDays(1), _rateLimitSettings.RequestsPerDay);

        // Try to consume tokens from all buckets
        if (!minuteBucket.TryConsume())
        {
            await HandleRateLimitExceeded(context, "minute", minuteBucket);
            return;
        }

        if (!hourBucket.TryConsume())
        {
            await HandleRateLimitExceeded(context, "hour", hourBucket);
            return;
        }

        if (!dayBucket.TryConsume())
        {
            await HandleRateLimitExceeded(context, "day", dayBucket);
            return;
        }

        // Track successful request
        _metrics.IncrementRequestCount(endpoint);

        // Add rate limit headers to response
        context.Response.OnStarting(() =>
        {
            context.Response.Headers["X-RateLimit-Limit-Minute"] = _rateLimitSettings.RequestsPerMinute.ToString();
            context.Response.Headers["X-RateLimit-Remaining-Minute"] = minuteBucket.AvailableTokens.ToString();
            context.Response.Headers["X-RateLimit-Limit-Hour"] = _rateLimitSettings.RequestsPerHour.ToString();
            context.Response.Headers["X-RateLimit-Remaining-Hour"] = hourBucket.AvailableTokens.ToString();
            return Task.CompletedTask;
        });

        await _next(context);
    }

    private bool ShouldApplyRateLimit(string path)
    {
        // Apply rate limiting to stock-related endpoints that use Yahoo Finance
        return path.Contains("/api/stocks");
    }

    private string GetEndpointKey(string path)
    {
        // Extract endpoint category for rate limiting
        if (path.Contains("/quote")) return "quote";
        if (path.Contains("/historical")) return "historical";
        if (path.Contains("/fundamentals")) return "fundamentals";
        if (path.Contains("/profile")) return "profile";
        if (path.Contains("/search")) return "search";
        return "general";
    }

    private TokenBucket GetOrCreateBucket(string key, TimeSpan window, int capacity)
    {
        return _buckets.GetOrAdd(key, _ => new TokenBucket(capacity, window));
    }

    private async Task HandleRateLimitExceeded(HttpContext context, string window, TokenBucket bucket)
    {
        var retryAfter = bucket.GetRetryAfterSeconds();
        
        _logger.LogWarning(
            "Rate limit exceeded for {Path}. Window: {Window}, Retry after: {RetryAfter}s",
            context.Request.Path,
            window,
            retryAfter);

        _metrics.IncrementRateLimitHits(GetEndpointKey(context.Request.Path.Value ?? string.Empty));

        context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
        context.Response.Headers["Retry-After"] = retryAfter.ToString();
        context.Response.Headers["X-RateLimit-Window"] = window;
        context.Response.ContentType = "application/json";

        var response = new
        {
            error = "Rate limit exceeded",
            message = $"Too many requests. Please retry after {retryAfter} seconds.",
            window = window,
            retryAfter = retryAfter
        };

        await context.Response.WriteAsJsonAsync(response);
    }

    /// <summary>
    /// Get current rate limit metrics
    /// </summary>
    public RateLimitMetrics GetMetrics() => _metrics;
}
