# Design Document - Alpha Vantage Integration

## Overview

This document describes the design for integrating Alpha Vantage as an alternative stock data provider while maintaining support for Yahoo Finance and enabling easy addition of future providers. The design uses the Strategy pattern to allow runtime provider selection and the Factory pattern for provider instantiation.

## Architecture

### High-Level Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                     API Layer                                │
│  (StocksController, HealthController)                       │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│                Application Layer                             │
│  (StockService - uses IStockDataProvider)                   │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│            Data Provider Strategy Layer                      │
│  ┌──────────────────────────────────────────────────────┐  │
│  │  IDataProviderStrategy                                │  │
│  │  - SelectProvider(context)                            │  │
│  │  - GetFallbackProvider()                              │  │
│  └──────────────────────────────────────────────────────┘  │
│                                                              │
│  Implementations:                                            │
│  - PrimaryProviderStrategy                                   │
│  - FallbackProviderStrategy                                  │
│  - RoundRobinProviderStrategy                                │
│  - CostOptimizedProviderStrategy                             │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│            Data Provider Factory                             │
│  ┌──────────────────────────────────────────────────────┐  │
│  │  IStockDataProviderFactory                            │  │
│  │  - CreateProvider(providerType)                       │  │
│  └──────────────────────────────────────────────────────┘  │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│              Data Provider Implementations                   │
│                                                              │
│  ┌──────────────────┐  ┌──────────────────┐               │
│  │ YahooFinance     │  │ AlphaVantage     │               │
│  │ Service          │  │ Service          │               │
│  │                  │  │                  │               │
│  │ Implements:      │  │ Implements:      │               │
│  │ IStockData       │  │ IStockData       │               │
│  │ Provider         │  │ Provider         │               │
│  └──────────────────┘  └──────────────────┘               │
│                                                              │
│  ┌──────────────────┐  ┌──────────────────┐               │
│  │ MockYahoo        │  │ [Future          │               │
│  │ FinanceService   │  │  Providers]      │               │
│  └──────────────────┘  └──────────────────┘               │
└─────────────────────────────────────────────────────────────┘
```

## Components and Interfaces

### 1. IStockDataProvider (Existing)

No changes needed - this interface remains the contract for all providers.

```csharp
public interface IStockDataProvider
{
    Task<MarketData> GetQuoteAsync(string symbol, CancellationToken cancellationToken = default);
    Task<List<MarketData>> GetQuotesAsync(List<string> symbols, CancellationToken cancellationToken = default);
    Task<List<StockPrice>> GetHistoricalPricesAsync(string symbol, DateTime startDate, DateTime endDate, TimeInterval interval = TimeInterval.Daily, CancellationToken cancellationToken = default);
    Task<FundamentalData> GetFundamentalsAsync(string symbol, CancellationToken cancellationToken = default);
    Task<CompanyProfile> GetCompanyProfileAsync(string symbol, CancellationToken cancellationToken = default);
    Task<List<StockSearchResult>> SearchSymbolsAsync(string query, int limit = 10, CancellationToken cancellationToken = default);
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);
}
```

### 2. IStockDataProviderFactory (New)

Factory for creating provider instances.

```csharp
public interface IStockDataProviderFactory
{
    IStockDataProvider CreateProvider(DataProviderType providerType);
    IStockDataProvider CreateProvider(string providerName);
    IEnumerable<DataProviderType> GetAvailableProviders();
}

public enum DataProviderType
{
    YahooFinance,
    AlphaVantage,
    Mock
}
```

### 3. IDataProviderStrategy (New)

Strategy for selecting which provider to use.

```csharp
public interface IDataProviderStrategy
{
    IStockDataProvider SelectProvider(DataProviderContext context);
    IStockDataProvider GetFallbackProvider();
    string GetStrategyName();
}

public class DataProviderContext
{
    public string Symbol { get; set; }
    public string Operation { get; set; } // "GetQuote", "GetHistorical", etc.
    public Dictionary<DataProviderType, ProviderHealth> ProviderHealth { get; set; }
    public Dictionary<DataProviderType, int> RateLimitRemaining { get; set; }
}

public class ProviderHealth
{
    public bool IsHealthy { get; set; }
    public DateTime LastChecked { get; set; }
    public int ConsecutiveFailures { get; set; }
    public TimeSpan AverageResponseTime { get; set; }
}
```

### 4. AlphaVantageService (New)

Implementation of IStockDataProvider for Alpha Vantage.

```csharp
public class AlphaVantageService : IStockDataProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AlphaVantageService> _logger;
    private readonly AlphaVantageSettings _settings;
    private readonly ICacheService _cacheService;
    private readonly IAlphaVantageRateLimiter _rateLimiter;
    
    // Implement all IStockDataProvider methods
}
```

### 5. Configuration Models

#### AlphaVantageSettings

```csharp
public class AlphaVantageSettings
{
    public const string SectionName = "AlphaVantage";
    
    public string ApiKey { get; set; }
    public string BaseUrl { get; set; } = "https://www.alphavantage.co/query";
    public int Timeout { get; set; } = 10;
    public int MaxRetries { get; set; } = 3;
    public RateLimitSettings RateLimit { get; set; } = new();
    public bool Enabled { get; set; } = false;
}
```

#### DataProviderSettings

```csharp
public class DataProviderSettings
{
    public const string SectionName = "DataProvider";
    
    public DataProviderType PrimaryProvider { get; set; } = DataProviderType.YahooFinance;
    public DataProviderType? FallbackProvider { get; set; }
    public ProviderStrategyType Strategy { get; set; } = ProviderStrategyType.Primary;
    public bool EnableAutomaticFallback { get; set; } = true;
    public int HealthCheckIntervalSeconds { get; set; } = 60;
}

public enum ProviderStrategyType
{
    Primary,        // Use only primary provider
    Fallback,       // Use primary, fallback on failure
    RoundRobin,     // Distribute load across providers
    CostOptimized   // Prefer cheaper providers
}
```

## Data Model Mapping

### Alpha Vantage to MarketData

Alpha Vantage Global Quote endpoint response:
```json
{
    "Global Quote": {
        "01. symbol": "AAPL",
        "02. open": "175.00",
        "03. high": "177.50",
        "04. low": "174.00",
        "05. price": "176.50",
        "06. volume": "50000000",
        "07. latest trading day": "2025-11-16",
        "08. previous close": "175.00",
        "09. change": "1.50",
        "10. change percent": "0.86%"
    }
}
```

Mapping:
```csharp
private MarketData MapToMarketData(AlphaVantageQuoteResponse response)
{
    var quote = response.GlobalQuote;
    return new MarketData
    {
        Symbol = quote.Symbol,
        CurrentPrice = decimal.Parse(quote.Price),
        PreviousClose = decimal.Parse(quote.PreviousClose),
        Open = decimal.Parse(quote.Open),
        DayHigh = decimal.Parse(quote.High),
        DayLow = decimal.Parse(quote.Low),
        Change = decimal.Parse(quote.Change),
        ChangePercent = decimal.Parse(quote.ChangePercent.TrimEnd('%')),
        Volume = long.Parse(quote.Volume),
        Timestamp = DateTime.Parse(quote.LatestTradingDay),
        MarketState = DetermineMarketState(DateTime.Parse(quote.LatestTradingDay))
    };
}
```

### Alpha Vantage to StockPrice

Alpha Vantage Time Series Daily endpoint:
```json
{
    "Time Series (Daily)": {
        "2025-11-16": {
            "1. open": "175.00",
            "2. high": "177.50",
            "3. low": "174.00",
            "4. close": "176.50",
            "5. volume": "50000000"
        }
    }
}
```

### Alpha Vantage to FundamentalData

Uses Company Overview endpoint for fundamental metrics.

### Alpha Vantage to CompanyProfile

Uses Company Overview endpoint for company information.

## Rate Limiting Strategy

### Alpha Vantage Rate Limits

**Free Tier:**
- 25 requests per day
- 5 requests per minute

**Premium Tier:**
- 500 requests per day (varies by plan)
- 75 requests per minute

### Implementation

```csharp
public interface IAlphaVantageRateLimiter
{
    Task<bool> TryAcquireAsync(CancellationToken cancellationToken = default);
    Task WaitForAvailabilityAsync(CancellationToken cancellationToken = default);
    RateLimitStatus GetStatus();
}

public class AlphaVantageRateLimiter : IAlphaVantageRateLimiter
{
    private readonly SemaphoreSlim _minuteSemaphore;
    private readonly SemaphoreSlim _daySemaphore;
    private readonly Timer _minuteResetTimer;
    private readonly Timer _dayResetTimer;
    
    // Token bucket implementation for minute and day limits
}
```

## Error Handling

### Error Types

1. **Authentication Errors** (Invalid API Key)
   - Return clear error message
   - Log warning
   - Don't retry

2. **Rate Limit Errors**
   - Return cached data if available
   - Queue request for later
   - Log warning with retry time

3. **Network Errors**
   - Retry with exponential backoff
   - Fallback to secondary provider
   - Log error with context

4. **Data Format Errors**
   - Log error with response body
   - Return null or throw specific exception
   - Alert for investigation

### Error Response Handling

```csharp
private async Task<T> HandleAlphaVantageResponse<T>(HttpResponseMessage response)
{
    if (!response.IsSuccessStatusCode)
    {
        var content = await response.Content.ReadAsStringAsync();
        
        if (content.Contains("Invalid API call"))
        {
            throw new InvalidApiKeyException("Alpha Vantage API key is invalid");
        }
        
        if (content.Contains("rate limit"))
        {
            throw new RateLimitExceededException("Alpha Vantage rate limit exceeded");
        }
        
        throw new ApiUnavailableException($"Alpha Vantage returned {response.StatusCode}");
    }
    
    // Parse and return
}
```

## Caching Strategy

### Cache Keys

```
Format: {provider}:{dataType}:{symbol}:{params}

Examples:
- alphavantage:quote:AAPL
- alphavantage:historical:AAPL:2025-01-01:2025-11-16:Daily
- alphavantage:fundamentals:AAPL
- alphavantage:profile:AAPL
- alphavantage:search:Apple
```

### TTL Configuration

```json
{
  "Cache": {
    "AlphaVantage": {
      "QuoteTTL": 900,        // 15 minutes
      "HistoricalTTL": 86400, // 24 hours
      "FundamentalsTTL": 21600, // 6 hours
      "ProfileTTL": 604800,   // 7 days
      "SearchTTL": 3600       // 1 hour
    }
  }
}
```

## Provider Selection Strategies

### 1. Primary Strategy

Always use configured primary provider.

```csharp
public class PrimaryProviderStrategy : IDataProviderStrategy
{
    public IStockDataProvider SelectProvider(DataProviderContext context)
    {
        return _factory.CreateProvider(_settings.PrimaryProvider);
    }
}
```

### 2. Fallback Strategy

Use primary, fallback to secondary on failure.

```csharp
public class FallbackProviderStrategy : IDataProviderStrategy
{
    public IStockDataProvider SelectProvider(DataProviderContext context)
    {
        var primary = _factory.CreateProvider(_settings.PrimaryProvider);
        
        if (context.ProviderHealth[_settings.PrimaryProvider].IsHealthy)
        {
            return primary;
        }
        
        return GetFallbackProvider();
    }
}
```

### 3. Round Robin Strategy

Distribute load evenly across providers.

```csharp
public class RoundRobinProviderStrategy : IDataProviderStrategy
{
    private int _currentIndex = 0;
    
    public IStockDataProvider SelectProvider(DataProviderContext context)
    {
        var providers = _factory.GetAvailableProviders()
            .Where(p => context.ProviderHealth[p].IsHealthy)
            .ToList();
            
        var provider = providers[_currentIndex % providers.Count];
        _currentIndex++;
        
        return _factory.CreateProvider(provider);
    }
}
```

### 4. Cost Optimized Strategy

Prefer cheaper providers when rate limits allow.

```csharp
public class CostOptimizedProviderStrategy : IDataProviderStrategy
{
    public IStockDataProvider SelectProvider(DataProviderContext context)
    {
        // Check if free tier provider has capacity
        if (context.RateLimitRemaining[DataProviderType.AlphaVantage] > 0)
        {
            return _factory.CreateProvider(DataProviderType.AlphaVantage);
        }
        
        // Fall back to Yahoo Finance (free, unlimited)
        return _factory.CreateProvider(DataProviderType.YahooFinance);
    }
}
```

## Health Monitoring

### Provider Health Tracking

```csharp
public class ProviderHealthMonitor
{
    private readonly Dictionary<DataProviderType, ProviderHealth> _healthStatus;
    private readonly Timer _healthCheckTimer;
    
    public async Task CheckHealthAsync(DataProviderType provider)
    {
        var providerInstance = _factory.CreateProvider(provider);
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var isHealthy = await providerInstance.IsHealthyAsync();
            stopwatch.Stop();
            
            _healthStatus[provider] = new ProviderHealth
            {
                IsHealthy = isHealthy,
                LastChecked = DateTime.UtcNow,
                ConsecutiveFailures = isHealthy ? 0 : _healthStatus[provider].ConsecutiveFailures + 1,
                AverageResponseTime = stopwatch.Elapsed
            };
        }
        catch
        {
            _healthStatus[provider].IsHealthy = false;
            _healthStatus[provider].ConsecutiveFailures++;
        }
    }
}
```

### Metrics Endpoint

Extend existing `/api/health/metrics` to include provider-specific metrics:

```json
{
  "providers": {
    "yahooFinance": {
      "totalRequests": 1500,
      "successfulRequests": 1450,
      "failedRequests": 50,
      "averageResponseTime": "250ms",
      "isHealthy": false,
      "lastHealthCheck": "2025-11-17T00:30:00Z"
    },
    "alphaVantage": {
      "totalRequests": 20,
      "successfulRequests": 20,
      "failedRequests": 0,
      "averageResponseTime": "180ms",
      "rateLimitRemaining": {
        "daily": 5,
        "minute": 3
      },
      "isHealthy": true,
      "lastHealthCheck": "2025-11-17T00:30:00Z"
    }
  },
  "currentProvider": "alphaVantage",
  "strategy": "Fallback"
}
```

## Configuration Example

### appsettings.json

```json
{
  "DataProvider": {
    "PrimaryProvider": "AlphaVantage",
    "FallbackProvider": "YahooFinance",
    "Strategy": "Fallback",
    "EnableAutomaticFallback": true,
    "HealthCheckIntervalSeconds": 60
  },
  "AlphaVantage": {
    "ApiKey": "",
    "BaseUrl": "https://www.alphavantage.co/query",
    "Timeout": 10,
    "MaxRetries": 3,
    "Enabled": true,
    "RateLimit": {
      "RequestsPerMinute": 5,
      "RequestsPerDay": 25
    }
  },
  "YahooFinance": {
    "BaseUrl": "https://query1.finance.yahoo.com",
    "Timeout": 10,
    "MaxRetries": 3,
    "Enabled": true,
    "UseMock": false
  }
}
```

### User Secrets (for API Key)

```bash
dotnet user-secrets set "AlphaVantage:ApiKey" "YOUR_API_KEY_HERE"
```

## Testing Strategy

### Unit Tests

- Test each provider implementation independently
- Mock HTTP responses
- Test data mapping functions
- Test error handling

### Integration Tests

- Test provider factory
- Test strategy selection
- Test fallback behavior
- Test rate limiting

### End-to-End Tests

- Test with real Alpha Vantage API (using test key)
- Test provider switching
- Test cache integration
- Test health monitoring

## Migration Path

### Phase 1: Add Infrastructure
- Create interfaces and base classes
- Implement factory pattern
- Add configuration models

### Phase 2: Implement Alpha Vantage
- Create AlphaVantageService
- Implement data mapping
- Add rate limiting

### Phase 3: Add Strategy Layer
- Implement provider strategies
- Add health monitoring
- Update dependency injection

### Phase 4: Testing & Validation
- Unit tests
- Integration tests
- Performance testing
- Documentation

### Phase 5: Deployment
- Update configuration
- Deploy to staging
- Monitor and validate
- Deploy to production

## Security Considerations

1. **API Key Storage**: Use User Secrets in development, Azure Key Vault in production
2. **API Key Logging**: Never log API keys
3. **Rate Limit Protection**: Prevent API key abuse
4. **Error Messages**: Don't expose API keys in errors
5. **HTTPS Only**: All API calls over HTTPS

## Performance Considerations

1. **Caching**: Aggressive caching to minimize API calls
2. **Connection Pooling**: Reuse HTTP connections
3. **Async/Await**: Non-blocking I/O operations
4. **Batch Requests**: Use batch endpoints when available
5. **Circuit Breaker**: Prevent cascading failures

## Cost Optimization

1. **Cache First**: Always check cache before API call
2. **Batch Operations**: Combine multiple requests
3. **Smart Fallback**: Use free providers when possible
4. **Rate Limit Awareness**: Stay within free tier limits
5. **Cost Tracking**: Monitor and alert on usage

## Future Enhancements

1. **Additional Providers**: IEX Cloud, Finnhub, Twelve Data
2. **Data Aggregation**: Combine data from multiple providers
3. **Smart Caching**: ML-based cache invalidation
4. **Cost Prediction**: Forecast monthly costs
5. **A/B Testing**: Compare provider data quality
