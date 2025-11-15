# Caching Implementation - Task 8

## Overview
This document describes the implementation of the cache-aside pattern in the StockService layer, integrating Redis caching with the Yahoo Finance data provider.

## Implementation Details

### Cache-Aside Pattern
The implementation follows the cache-aside (lazy loading) pattern:
1. Check cache first for requested data
2. If cache hit: return cached data
3. If cache miss: fetch from API, store in cache, return data

### Cache Keys
The following cache key patterns are used:

| Data Type | Cache Key Pattern | Example |
|-----------|------------------|---------|
| Quote | `quote:{symbol}` | `quote:AAPL` |
| Historical | `historical:{symbol}:{start}:{end}:{interval}` | `historical:AAPL:2024-01-01:2024-12-31:Daily` |
| Fundamentals | `fundamentals:{symbol}` | `fundamentals:AAPL` |
| Profile | `profile:{symbol}` | `profile:AAPL` |
| Search | `search:{query}` | `search:apple` |

### TTL (Time To Live) Values
Different data types have different cache expiration times based on how frequently they change:

| Data Type | TTL | Reason |
|-----------|-----|--------|
| Quote | 15 minutes (900s) | Market data changes frequently during trading hours |
| Historical | 24 hours (86400s) | Historical data is static once the trading day ends |
| Fundamentals | 6 hours (21600s) | Financial metrics update quarterly but may have intraday adjustments |
| Profile | 7 days (604800s) | Company information rarely changes |
| Search | 1 hour (3600s) | Search results are relatively stable but may need periodic refresh |

## New Methods Added to StockService

### 1. GetQuoteAsync
```csharp
Task<MarketData> GetQuoteAsync(string symbol, CancellationToken cancellationToken = default)
```
Fetches current market data with 15-minute caching.

### 2. GetHistoricalPricesAsync
```csharp
Task<List<StockPrice>> GetHistoricalPricesAsync(
    string symbol,
    DateTime startDate,
    DateTime endDate,
    TimeInterval interval = TimeInterval.Daily,
    CancellationToken cancellationToken = default)
```
Fetches historical price data with 24-hour caching.

### 3. GetFundamentalsAsync
```csharp
Task<FundamentalData> GetFundamentalsAsync(string symbol, CancellationToken cancellationToken = default)
```
Fetches fundamental data with 6-hour caching.

### 4. GetCompanyProfileAsync
```csharp
Task<CompanyProfile> GetCompanyProfileAsync(string symbol, CancellationToken cancellationToken = default)
```
Fetches company profile with 7-day caching.

### 5. SearchSymbolsAsync
```csharp
Task<List<StockSearchResult>> SearchSymbolsAsync(
    string query,
    int limit = 10,
    CancellationToken cancellationToken = default)
```
Searches for symbols with 1-hour caching.

## Architecture Changes

### Interface Updates
- **ICacheService**: Moved from Infrastructure to Core layer to maintain proper dependency flow
- **IStockService**: Extended with new cached data provider methods

### Dependency Injection
The StockService now requires three dependencies:
1. `IStockRepository` - For database operations
2. `IStockDataProvider` - For fetching data from external APIs (Yahoo Finance)
3. `ICacheService` - For Redis caching operations

### Service Registration (Program.cs)
```csharp
// Register IStockDataProvider (using YahooFinanceService as the implementation)
builder.Services.AddScoped<IStockDataProvider>(sp => sp.GetRequiredService<IYahooFinanceService>());
```

## Logging
Each cached method logs:
- Cache HIT events (when data is found in cache)
- Cache MISS events (when data needs to be fetched from API)
- Cache storage operations with TTL information
- Errors during fetch operations

Example log output:
```
[Information] Cache MISS for quote AAPL. Fetching from API
[Information] Cached quote for AAPL with TTL 900s
[Information] Cache HIT for quote AAPL
```

## Benefits

1. **Reduced API Calls**: Significantly reduces calls to Yahoo Finance API, staying within rate limits
2. **Improved Performance**: Cached responses are much faster than API calls
3. **Cost Savings**: Fewer API calls mean lower costs if using paid data providers
4. **Resilience**: Cache can serve as fallback when API is temporarily unavailable (future enhancement)
5. **Scalability**: Distributed Redis cache supports horizontal scaling

## Requirements Satisfied

This implementation satisfies the following requirements from the requirements document:

- **1.4**: Cache market data for 15 minutes
- **2.5**: Cache historical data for 24 hours
- **4.5**: Cache company profile data for 7 days
- **7.1**: Check Redis cache before making API calls
- **7.2**: Use different TTL values based on data type
- **8.5**: Cache search results for 1 hour

## Testing

The existing unit tests have been updated to include mock dependencies for:
- `IStockDataProvider`
- `ICacheService`

All tests pass successfully, confirming backward compatibility with existing functionality.

## Next Steps

Future enhancements (covered in subsequent tasks):
- Task 9: Implement fallback to cache on API failures
- Task 10: Add rate limiting middleware
- Task 11: Add comprehensive logging for cache hit/miss statistics
