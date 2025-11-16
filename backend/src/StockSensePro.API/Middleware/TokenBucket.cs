namespace StockSensePro.API.Middleware;

/// <summary>
/// Token bucket implementation for rate limiting
/// </summary>
public class TokenBucket
{
    private readonly int _capacity;
    private readonly TimeSpan _refillInterval;
    private readonly object _lock = new();
    private int _availableTokens;
    private DateTime _lastRefillTime;

    public TokenBucket(int capacity, TimeSpan refillInterval)
    {
        _capacity = capacity;
        _refillInterval = refillInterval;
        _availableTokens = capacity;
        _lastRefillTime = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the number of available tokens
    /// </summary>
    public int AvailableTokens
    {
        get
        {
            lock (_lock)
            {
                RefillTokens();
                return _availableTokens;
            }
        }
    }

    /// <summary>
    /// Attempts to consume a token from the bucket
    /// </summary>
    /// <returns>True if token was consumed, false if bucket is empty</returns>
    public bool TryConsume()
    {
        lock (_lock)
        {
            RefillTokens();

            if (_availableTokens > 0)
            {
                _availableTokens--;
                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// Gets the number of seconds until the bucket refills
    /// </summary>
    public int GetRetryAfterSeconds()
    {
        lock (_lock)
        {
            var timeSinceLastRefill = DateTime.UtcNow - _lastRefillTime;
            var timeUntilRefill = _refillInterval - timeSinceLastRefill;
            return Math.Max(1, (int)Math.Ceiling(timeUntilRefill.TotalSeconds));
        }
    }

    private void RefillTokens()
    {
        var now = DateTime.UtcNow;
        var timeSinceLastRefill = now - _lastRefillTime;

        // If the refill interval has passed, reset the bucket
        if (timeSinceLastRefill >= _refillInterval)
        {
            _availableTokens = _capacity;
            _lastRefillTime = now;
        }
    }
}
