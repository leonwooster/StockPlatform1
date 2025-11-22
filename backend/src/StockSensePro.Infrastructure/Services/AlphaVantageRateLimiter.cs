using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StockSensePro.Core.Configuration;
using StockSensePro.Core.Interfaces;

namespace StockSensePro.Infrastructure.Services;

/// <summary>
/// Rate limiter for Alpha Vantage API using token bucket algorithm
/// Enforces both per-minute and per-day rate limits
/// </summary>
public class AlphaVantageRateLimiter : IAlphaVantageRateLimiter, IDisposable
{
    private readonly ILogger<AlphaVantageRateLimiter> _logger;
    private readonly RateLimitSettings _settings;

    // Token buckets for minute and day limits
    private int _minuteTokens;
    private int _dayTokens;

    // Locks for thread-safe token management
    private readonly SemaphoreSlim _minuteLock = new(1, 1);
    private readonly SemaphoreSlim _dayLock = new(1, 1);

    // Timers for automatic token refill
    private readonly Timer _minuteResetTimer;
    private readonly Timer _dayResetTimer;

    // Track when windows reset
    private DateTime _minuteWindowResetTime;
    private DateTime _dayWindowResetTime;

    private bool _disposed;

    public AlphaVantageRateLimiter(
        ILogger<AlphaVantageRateLimiter> logger,
        IOptions<AlphaVantageSettings> settings)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settings = settings?.Value?.RateLimit ?? throw new ArgumentNullException(nameof(settings));

        // Initialize token buckets to full capacity
        _minuteTokens = _settings.RequestsPerMinute;
        _dayTokens = _settings.RequestsPerDay;

        // Set initial reset times
        _minuteWindowResetTime = DateTime.UtcNow.AddMinutes(1);
        _dayWindowResetTime = DateTime.UtcNow.Date.AddDays(1);

        // Create timers for automatic token refill
        // Minute timer: refill every minute
        _minuteResetTimer = new Timer(
            RefillMinuteTokens,
            null,
            TimeSpan.FromMinutes(1),
            TimeSpan.FromMinutes(1));

        // Day timer: refill at midnight UTC
        var timeUntilMidnight = _dayWindowResetTime - DateTime.UtcNow;
        _dayResetTimer = new Timer(
            RefillDayTokens,
            null,
            timeUntilMidnight,
            TimeSpan.FromDays(1));

        _logger.LogInformation(
            "AlphaVantageRateLimiter initialized: RequestsPerMinute={RequestsPerMinute}, RequestsPerDay={RequestsPerDay}",
            _settings.RequestsPerMinute,
            _settings.RequestsPerDay);
    }

    public async Task<bool> TryAcquireAsync(CancellationToken cancellationToken = default)
    {
        // Check both minute and day limits
        var hasMinuteToken = await TryAcquireMinuteTokenAsync(cancellationToken);
        if (!hasMinuteToken)
        {
            _logger.LogWarning(
                "Rate limit exceeded: Minute limit reached ({Limit} requests/minute). Reset in {ResetIn}",
                _settings.RequestsPerMinute,
                _minuteWindowResetTime - DateTime.UtcNow);
            return false;
        }

        var hasDayToken = await TryAcquireDayTokenAsync(cancellationToken);
        if (!hasDayToken)
        {
            // Return the minute token since we couldn't get a day token
            await ReturnMinuteTokenAsync();

            _logger.LogWarning(
                "Rate limit exceeded: Daily limit reached ({Limit} requests/day). Reset in {ResetIn}",
                _settings.RequestsPerDay,
                _dayWindowResetTime - DateTime.UtcNow);
            return false;
        }

        _logger.LogDebug(
            "Rate limit token acquired: MinuteRemaining={MinuteRemaining}, DayRemaining={DayRemaining}",
            _minuteTokens,
            _dayTokens);

        return true;
    }

    public async Task WaitForAvailabilityAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var attemptCount = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            attemptCount++;

            if (await TryAcquireAsync(cancellationToken))
            {
                stopwatch.Stop();
                _logger.LogInformation(
                    "Rate limit token acquired after waiting {ElapsedMs}ms ({Attempts} attempts)",
                    stopwatch.ElapsedMilliseconds,
                    attemptCount);
                return;
            }

            // Calculate wait time based on which limit is blocking
            var status = GetStatus();
            var waitTime = TimeSpan.Zero;

            if (status.MinuteRequestsRemaining <= 0)
            {
                waitTime = status.MinuteWindowResetIn;
                _logger.LogDebug(
                    "Waiting for minute window reset in {WaitTimeSeconds}s (attempt {Attempt})",
                    waitTime.TotalSeconds,
                    attemptCount);
            }
            else if (status.DayRequestsRemaining <= 0)
            {
                waitTime = status.DayWindowResetIn;
                _logger.LogDebug(
                    "Waiting for day window reset in {WaitTimeSeconds}s (attempt {Attempt})",
                    waitTime.TotalSeconds,
                    attemptCount);
            }

            // Add a small buffer to ensure the window has reset
            waitTime = waitTime.Add(TimeSpan.FromMilliseconds(100));

            // Wait for the calculated time or until cancellation
            try
            {
                await Task.Delay(waitTime, cancellationToken);
            }
            catch (TaskCanceledException)
            {
                _logger.LogInformation("Rate limit wait cancelled after {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                throw;
            }
        }

        cancellationToken.ThrowIfCancellationRequested();
    }

    public RateLimitStatus GetStatus()
    {
        var now = DateTime.UtcNow;

        return new RateLimitStatus
        {
            MinuteRequestsRemaining = _minuteTokens,
            MinuteRequestsLimit = _settings.RequestsPerMinute,
            MinuteWindowResetIn = _minuteWindowResetTime > now
                ? _minuteWindowResetTime - now
                : TimeSpan.Zero,

            DayRequestsRemaining = _dayTokens,
            DayRequestsLimit = _settings.RequestsPerDay,
            DayWindowResetIn = _dayWindowResetTime > now
                ? _dayWindowResetTime - now
                : TimeSpan.Zero
        };
    }

    private async Task<bool> TryAcquireMinuteTokenAsync(CancellationToken cancellationToken)
    {
        await _minuteLock.WaitAsync(cancellationToken);
        try
        {
            if (_minuteTokens > 0)
            {
                _minuteTokens--;
                return true;
            }
            return false;
        }
        finally
        {
            _minuteLock.Release();
        }
    }

    private async Task<bool> TryAcquireDayTokenAsync(CancellationToken cancellationToken)
    {
        await _dayLock.WaitAsync(cancellationToken);
        try
        {
            if (_dayTokens > 0)
            {
                _dayTokens--;
                return true;
            }
            return false;
        }
        finally
        {
            _dayLock.Release();
        }
    }

    private async Task ReturnMinuteTokenAsync()
    {
        await _minuteLock.WaitAsync();
        try
        {
            if (_minuteTokens < _settings.RequestsPerMinute)
            {
                _minuteTokens++;
            }
        }
        finally
        {
            _minuteLock.Release();
        }
    }

    private void RefillMinuteTokens(object? state)
    {
        _minuteLock.Wait();
        try
        {
            var previousTokens = _minuteTokens;
            _minuteTokens = _settings.RequestsPerMinute;
            _minuteWindowResetTime = DateTime.UtcNow.AddMinutes(1);

            _logger.LogDebug(
                "Minute rate limit window reset: Tokens refilled from {Previous} to {Current}",
                previousTokens,
                _minuteTokens);
        }
        finally
        {
            _minuteLock.Release();
        }
    }

    private void RefillDayTokens(object? state)
    {
        _dayLock.Wait();
        try
        {
            var previousTokens = _dayTokens;
            _dayTokens = _settings.RequestsPerDay;
            _dayWindowResetTime = DateTime.UtcNow.Date.AddDays(1);

            _logger.LogInformation(
                "Daily rate limit window reset: Tokens refilled from {Previous} to {Current}",
                previousTokens,
                _dayTokens);
        }
        finally
        {
            _dayLock.Release();
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _minuteResetTimer?.Dispose();
        _dayResetTimer?.Dispose();
        _minuteLock?.Dispose();
        _dayLock?.Dispose();

        _disposed = true;
        _logger.LogDebug("AlphaVantageRateLimiter disposed");
    }
}
