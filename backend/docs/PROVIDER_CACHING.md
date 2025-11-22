# Provider-Specific Caching Implementation

## Overview

The StockSensePro application implements a provider-specific caching strategy that allows different cache TTLs (Time To Live) for each data provider. This enables fine-tuned cache management based on the characteristics and rate limits of each provider.

## Cache Key Format

All cache keys follow a consistent format:

```
{provider}:{dataType}:{symbol}[:{additionalParams}]
```

### Examples

- **Quote Data**: `alphavantage:quote:AAPL`
- **Historical Data**: `alphavantage:historical:AAPL:2025-01-01:2025-11-16:Daily`
- **Fundamentals**: `alphavantage:fundamentals:AAPL`
- **Company Profile**: `alphavantage:profile:AAPL`
- **Search Results**: `alphavantage:search:Apple:10`
- **Calculated Fields**: `alphavantage:calculated:AAPL`

## Configuration

Provider-specific cache TTLs are configured in `appsettings.json` under the `Cache` section:

```json
{
  "Cache": {
    "DefaultTTL": 900,
    "QuoteTTL": 900,
    "HistoricalTTL": 86400,
    "FundamentalsTTL": 21600,
    "ProfileTTL": 604800,
    "SearchTTL": 3600,
    "EnableCacheWarming": false,
    "MaxCacheSize": 10000,
    "AlphaVantage": {
      "QuoteTTL": 900,
      "HistoricalTTL": 86400,
      "FundamentalsTTL": 21600,
      "ProfileTTL": 604800,
      "SearchTTL": 3600,
      "CalculatedFieldsTTL": 86400
    },
    "YahooFinance": {
      "QuoteTTL": 900,
      "HistoricalTTL": 86400,
      "FundamentalsTTL": 21600,
      "ProfileTTL": 604800,
      "SearchTTL": 3600,
      "CalculatedFieldsTTL": 86400
    }
  }
}
```

### TTL Values (in seconds)

| Data Type | Default TTL | Description |
|-----------|-------------|-------------|
| Quote | 900 (15 min) | Real-time market data |
| Historical | 86400 (24 hours) | Historical price data |
| Fundamentals | 21600 (6 hours) | Company fundamental metrics |
| Profile | 604800 (7 days) | Company profile information |
| Search | 3600 (1 hour) | Symbol search results |
| Calculated Fields | 86400 (24 hours) | Derived metrics (52-week range, avg volume) |

## Cache-Aside Pattern

The implementation uses the cache-aside (lazy loading) pattern:

1. **Read Operation**:
   - Check cache for data
   - If cache hit: return cached data
   - If cache miss: fetch from provider API
   - Store fetched data in cache with provider-specific TTL
   - Return data

2. **Rate Limit Handling**:
   - If rate limit exceeded: return cached data (even if stale)
   - Log warning about using stale cache
   - If no cached data available: throw RateLimitExceededException

## Implementation Details

### AlphaVantageService

The `AlphaVantageService` class implements provider-specific caching:

```csharp
public class AlphaVantageService : IStockDataProvider
{
    private readonly CacheSettings _cacheSettings;
    private readonly ICacheService? _cacheService;

    public async Task<MarketData> GetQuoteAsync(string symbol, CancellationToken cancellationToken = default)
    {
        // Check cache first
        var cacheKey = $"alphavantage:quote:{symbol}";
        MarketData? cachedData = null;
        
        if (_cacheService != null)
        {
            cachedData = await _cacheService.GetAsync<MarketData>(cacheKey);
            if (cachedData != null)
            {
                return cachedData; // Cache hit
            }
        }

        try
        {
            // Fetch from API
            var data = await FetchFromApiAsync(symbol, cancellationToken);
            
            // Cache with provider-specific TTL
            if (_cacheService != null)
            {
                var ttl = TimeSpan.FromSeconds(_cacheSettings.AlphaVantage.QuoteTTL);
                await _cacheService.SetAsync(cacheKey, data, ttl);
            }
            
            return data;
        }
        catch (RateLimitExceededException)
        {
            // Return stale cache if available
            if (cachedData != null)
            {
                return cachedData;
            }
            throw;
        }
    }
}
```

### RedisCacheService

The `RedisCacheService` provides cache hit/miss logging:

```csharp
public class RedisCacheService : ICacheService
{
    public async Task<T?> GetAsync<T>(string key)
    {
        var value = await _database.StringGetAsync(key);
        
        if (value.IsNullOrEmpty)
        {
            _logger.LogInformation("Cache MISS: Key={Key}", key);
            return default(T);
        }
        
        _logger.LogInformation("Cache HIT: Key={Key}, DataSize={DataSizeBytes}bytes", 
            key, value.Length());
        
        return JsonSerializer.Deserialize<T>(value!);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        var json = JsonSerializer.Serialize(value);
        await _database.StringSetAsync(key, json, expiry);
        
        _logger.LogInformation("Cache SET: Key={Key}, TTL={TTLSeconds}s", 
            key, expiry?.TotalSeconds ?? -1);
    }
}
```

## Cache Monitoring

### Log Messages

The caching implementation provides detailed logging:

- **Cache HIT**: `Cache HIT: Key=alphavantage:quote:AAPL, DataSize=1234bytes, ResponseTime=5ms`
- **Cache MISS**: `Cache MISS: Key=alphavantage:quote:AAPL, ResponseTime=3ms`
- **Cache SET**: `Cache SET: Key=alphavantage:quote:AAPL, DataSize=1234bytes, TTL=900s, ResponseTime=8ms`
- **Cached Data**: `Cached quote for AAPL with TTL 900s`

### Metrics

Monitor cache effectiveness through:

1. **Cache Hit Rate**: Percentage of requests served from cache
2. **Cache Size**: Number of items in cache
3. **TTL Effectiveness**: How often cached data is used before expiration
4. **Rate Limit Avoidance**: Requests served from cache during rate limiting

## Best Practices

### TTL Configuration

1. **Real-time Data** (quotes): Short TTL (5-15 minutes)
   - Balances freshness with API usage
   - Reduces load during market hours

2. **Historical Data**: Long TTL (24 hours)
   - Historical data doesn't change
   - Minimizes API calls for backtesting

3. **Fundamental Data**: Medium TTL (6 hours)
   - Updates quarterly but accessed frequently
   - Good balance for analysis workloads

4. **Profile Data**: Very long TTL (7 days)
   - Rarely changes
   - Safe to cache for extended periods

5. **Calculated Fields**: Long TTL (24 hours)
   - Expensive to compute
   - Acceptable staleness for derived metrics

### Rate Limit Management

1. **Cache First**: Always check cache before API calls
2. **Stale Cache Fallback**: Return stale data when rate limited
3. **Graceful Degradation**: Provide partial data when possible
4. **User Notification**: Inform users when serving stale data

### Performance Optimization

1. **Batch Operations**: Cache individual items for reuse
2. **Parallel Caching**: Cache enrichment data separately
3. **Cache Warming**: Pre-populate cache for popular symbols
4. **Cache Invalidation**: Remove stale data proactively

## Future Enhancements

1. **Smart TTL**: Adjust TTL based on market hours and volatility
2. **Cache Warming**: Pre-fetch data for popular symbols
3. **Distributed Caching**: Scale across multiple instances
4. **Cache Analytics**: Track hit rates and optimize TTLs
5. **Conditional Caching**: Skip cache for real-time critical operations

## Related Documentation

- [Alpha Vantage Integration Design](../../../.kiro/specs/alpha-vantage-integration/design.md)
- [Alpha Vantage Integration Requirements](../../../.kiro/specs/alpha-vantage-integration/requirements.md)
- [Rate Limiting Implementation](./RATE_LIMITING.md)
