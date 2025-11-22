# Alpha Vantage Integration - Developer Guide

## Table of Contents

1. [Overview](#overview)
2. [Provider Implementation Guide](#provider-implementation-guide)
3. [Adding New Providers](#adding-new-providers)
4. [Testing Strategies](#testing-strategies)
5. [Troubleshooting Guide](#troubleshooting-guide)
6. [Architecture Deep Dive](#architecture-deep-dive)
7. [Best Practices](#best-practices)

---

## Overview

This guide is intended for developers working on the StockSensePro multi-provider architecture. It covers the implementation details, extension points, testing approaches, and troubleshooting techniques for the Alpha Vantage integration and the broader provider system.

### Key Concepts

- **Provider**: A service that implements `IStockDataProvider` to fetch stock market data from a specific source
- **Strategy**: A pattern for selecting which provider to use for each request (implements `IDataProviderStrategy`)
- **Factory**: Creates provider instances based on configuration (implements `IStockDataProviderFactory`)
- **Health Monitor**: Tracks provider health and availability (implements `IProviderHealthMonitor`)
- **Rate Limiter**: Enforces API rate limits to prevent overages (implements `IAlphaVantageRateLimiter`)
- **Cost Tracker**: Monitors API usage and calculates costs (implements `IProviderCostTracker`)

### Architecture Layers

```
┌─────────────────────────────────────────────────────────────┐
│                     API Layer                                │
│  Controllers: StocksController, HealthController            │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│                Application Layer                             │
│  Services: StockService (uses IDataProviderStrategy)        │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│            Strategy Layer                                    │
│  Strategies: Primary, Fallback, RoundRobin, CostOptimized  │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│            Factory Layer                                     │
│  Factory: StockDataProviderFactory                          │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│              Provider Implementations                        │
│  Providers: YahooFinanceService, AlphaVantageService, Mock  │
└─────────────────────────────────────────────────────────────┘
```

---

## Provider Implementation Guide

### Understanding IStockDataProvider

All providers must implement the `IStockDataProvider` interface:

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

### Alpha Vantage Implementation Structure

The `AlphaVantageService` class demonstrates best practices for provider implementation:

#### 1. Constructor Injection

```csharp
public AlphaVantageService(
    HttpClient httpClient,
    ILogger<AlphaVantageService> logger,
    IOptions<AlphaVantageSettings> settings,
    IOptions<CacheSettings> cacheSettings,
    ICacheService? cacheService = null,
    IStockDataProvider? yahooFinanceProvider = null,
    IAlphaVantageRateLimiter? rateLimiter = null)
{
    // Dependencies are injected, with optional services for enrichment
}
```

**Key Points**:
- Use `HttpClient` for API calls (configured with Polly policies in `Program.cs`)
- Inject `ILogger` for structured logging
- Use `IOptions<T>` pattern for configuration
- Optional dependencies allow for flexible feature enablement

#### 2. Request Execution Pattern

```csharp
private async Task<HttpResponseMessage> ExecuteRequestWithLoggingAsync(
    string endpoint,
    string symbol,
    Dictionary<string, string> parameters,
    CancellationToken cancellationToken = default)
{
    // 1. Check rate limits
    if (_rateLimiter != null)
    {
        var acquired = await _rateLimiter.TryAcquireAsync(cancellationToken);
        if (!acquired)
        {
            throw new RateLimitExceededException(...);
        }
    }
    
    // 2. Build request URL
    parameters["apikey"] = _settings.ApiKey;
    var queryString = string.Join("&", parameters.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
    
    // 3. Implement retry logic with exponential backoff
    var maxRetries = _settings.MaxRetries;
    var retryCount = 0;
    
    while (retryCount <= maxRetries)
    {
        try
        {
            // 4. Log request details
            _logger.LogInformation("Alpha Vantage API Request: Endpoint={Endpoint}, Symbol={Symbol}...", endpoint, symbol);
            
            // 5. Execute request
            var response = await _httpClient.GetAsync(requestUrl, cancellationToken);
            
            // 6. Log response details
            _logger.LogInformation("Alpha Vantage API Response: StatusCode={StatusCode}...", response.StatusCode);
            
            return response;
        }
        catch (HttpRequestException ex) when (retryCount < maxRetries)
        {
            // Retry on transient errors
            retryCount++;
            await Task.Delay((int)Math.Pow(2, retryCount) * 100, cancellationToken);
        }
    }
}
```

**Best Practices**:
- Always check rate limits before making requests
- Implement exponential backoff for retries
- Log all requests and responses with structured data
- Handle transient errors gracefully

#### 3. Caching Pattern

```csharp
public async Task<MarketData> GetQuoteAsync(string symbol, CancellationToken cancellationToken = default)
{
    // 1. Check cache first
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
        // 2. Fetch from API
        var response = await ExecuteRequestWithLoggingAsync(...);
        var marketData = MapToMarketData(response);
        
        // 3. Cache the result
        if (_cacheService != null)
        {
            var ttl = TimeSpan.FromSeconds(_cacheSettings.AlphaVantage.QuoteTTL);
            await _cacheService.SetAsync(cacheKey, marketData, ttl);
        }
        
        return marketData;
    }
    catch (RateLimitExceededException)
    {
        // 4. Return cached data on rate limit (even if stale)
        if (cachedData != null)
        {
            _logger.LogWarning("Rate limit exceeded, returning cached data");
            return cachedData;
        }
        throw;
    }
}
```

**Cache Key Format**: `{provider}:{dataType}:{symbol}:{params}`

Examples:
- `alphavantage:quote:AAPL`
- `alphavantage:historical:MSFT:2025-01-01:2025-11-22:Daily`
- `alphavantage:fundamentals:GOOGL`

#### 4. Error Handling

```csharp
private Task HandleErrorResponseAsync(HttpResponseMessage response, string symbol, string content, CancellationToken cancellationToken)
{
    // Check for authentication errors
    if (content.Contains("Invalid API key", StringComparison.OrdinalIgnoreCase))
    {
        throw new InvalidApiKeyException("Invalid Alpha Vantage API key", "AlphaVantage");
    }
    
    // Check for rate limit errors
    if (content.Contains("rate limit", StringComparison.OrdinalIgnoreCase))
    {
        throw new RateLimitExceededException($"Alpha Vantage rate limit exceeded for {symbol}", symbol);
    }
    
    // Check for symbol not found
    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
    {
        throw new SymbolNotFoundException(symbol);
    }
    
    // Generic API error
    throw new ApiUnavailableException($"Alpha Vantage API error for {symbol}", symbol);
}
```

**Custom Exceptions**:
- `InvalidApiKeyException`: Authentication failure
- `RateLimitExceededException`: Rate limit hit (triggers cache fallback)
- `SymbolNotFoundException`: Invalid symbol
- `ApiUnavailableException`: Generic API error (triggers provider fallback)

#### 5. Data Enrichment

Alpha Vantage doesn't provide all fields that Yahoo Finance does. The service implements data enrichment:

```csharp
private async Task EnrichMarketDataAsync(MarketData marketData, string symbol, CancellationToken cancellationToken)
{
    // Enrich with bid/ask from Yahoo Finance
    if (_settings.DataEnrichment.EnableBidAskEnrichment && _yahooFinanceProvider != null)
    {
        var (bidPrice, askPrice) = await EnrichWithYahooFinanceBidAskAsync(symbol, _yahooFinanceProvider, cancellationToken);
        marketData.BidPrice = bidPrice;
        marketData.AskPrice = askPrice;
    }
    
    // Calculate 52-week range from historical data
    if (_settings.DataEnrichment.EnableCalculated52WeekRange)
    {
        var (high, low) = await Calculate52WeekRangeAsync(symbol, cancellationToken);
        marketData.FiftyTwoWeekHigh = high;
        marketData.FiftyTwoWeekLow = low;
    }
    
    // Calculate average volume
    if (_settings.DataEnrichment.EnableCalculatedAverageVolume)
    {
        marketData.AverageVolume = await CalculateAverageVolumeAsync(symbol, cancellationToken);
    }
}
```

**Enrichment Features**:
- Hybrid data: Supplement with data from other providers
- Calculated fields: Derive missing fields from available data
- Cached calculations: Cache expensive calculations separately

---

## Adding New Providers

### Step-by-Step Guide

#### Step 1: Create Provider Enum Value

Add your provider to `DataProviderType` enum:

```csharp
// File: StockSensePro.Core/Enums/DataProviderType.cs
public enum DataProviderType
{
    YahooFinance,
    AlphaVantage,
    Mock,
    IEXCloud,      // New provider
    Finnhub        // New provider
}
```

#### Step 2: Create Configuration Class

```csharp
// File: StockSensePro.Core/Configuration/IEXCloudSettings.cs
public class IEXCloudSettings
{
    public const string SectionName = "IEXCloud";
    
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://cloud.iexapis.com/stable";
    public int Timeout { get; set; } = 10;
    public int MaxRetries { get; set; } = 3;
    public bool Enabled { get; set; } = false;
    
    public RateLimitSettings RateLimit { get; set; } = new();
}
```

#### Step 3: Implement IStockDataProvider

```csharp
// File: StockSensePro.Infrastructure/Services/IEXCloudService.cs
public class IEXCloudService : IStockDataProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<IEXCloudService> _logger;
    private readonly IEXCloudSettings _settings;
    private readonly ICacheService? _cacheService;
    
    public IEXCloudService(
        HttpClient httpClient,
        ILogger<IEXCloudService> logger,
        IOptions<IEXCloudSettings> settings,
        ICacheService? cacheService = null)
    {
        _httpClient = httpClient;
        _logger = logger;
        _settings = settings.Value;
        _cacheService = cacheService;
    }
    
    public async Task<MarketData> GetQuoteAsync(string symbol, CancellationToken cancellationToken = default)
    {
        // 1. Check cache
        var cacheKey = $"iexcloud:quote:{symbol}";
        var cached = await _cacheService?.GetAsync<MarketData>(cacheKey);
        if (cached != null) return cached;
        
        // 2. Make API call
        var url = $"/stock/{symbol}/quote?token={_settings.ApiKey}";
        var response = await _httpClient.GetAsync(url, cancellationToken);
        
        // 3. Parse and map response
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var data = JsonSerializer.Deserialize<IEXCloudQuoteResponse>(json);
        var marketData = MapToMarketData(data);
        
        // 4. Cache result
        await _cacheService?.SetAsync(cacheKey, marketData, TimeSpan.FromMinutes(15));
        
        return marketData;
    }
    
    // Implement other interface methods...
}
```

#### Step 4: Update Provider Factory

```csharp
// File: StockSensePro.Infrastructure/Services/StockDataProviderFactory.cs
public IStockDataProvider CreateProvider(DataProviderType providerType)
{
    return providerType switch
    {
        DataProviderType.YahooFinance => _serviceProvider.GetRequiredService<IYahooFinanceService>(),
        DataProviderType.AlphaVantage => _serviceProvider.GetRequiredService<AlphaVantageService>(),
        DataProviderType.Mock => _serviceProvider.GetRequiredService<MockYahooFinanceService>(),
        DataProviderType.IEXCloud => _serviceProvider.GetRequiredService<IEXCloudService>(),  // Add this
        _ => throw new ArgumentException($"Unknown provider type: {providerType}")
    };
}

public IEnumerable<DataProviderType> GetAvailableProviders()
{
    var providers = new List<DataProviderType>();
    
    if (_yahooFinanceSettings.Enabled) providers.Add(DataProviderType.YahooFinance);
    if (_alphaVantageSettings.Enabled) providers.Add(DataProviderType.AlphaVantage);
    if (_iexCloudSettings.Enabled) providers.Add(DataProviderType.IEXCloud);  // Add this
    providers.Add(DataProviderType.Mock); // Always available
    
    return providers;
}
```

#### Step 5: Register in Dependency Injection

```csharp
// File: Program.cs

// Bind configuration
var iexCloudSettings = builder.Configuration
    .GetSection(IEXCloudSettings.SectionName)
    .Get<IEXCloudSettings>() ?? new IEXCloudSettings();

builder.Services.AddSingleton(iexCloudSettings);
builder.Services.Configure<IEXCloudSettings>(builder.Configuration.GetSection(IEXCloudSettings.SectionName));

// Configure HttpClient with Polly policies
builder.Services.AddHttpClient<IEXCloudService>(client =>
{
    client.BaseAddress = new Uri(iexCloudSettings.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(iexCloudSettings.Timeout);
})
.AddPolicyHandler(GetRetryPolicy(iexCloudSettings.MaxRetries))
.AddPolicyHandler(GetCircuitBreakerPolicy())
.AddPolicyHandler(GetTimeoutPolicy(TimeSpan.FromSeconds(iexCloudSettings.Timeout)));

// Register service
builder.Services.AddScoped<IEXCloudService>();
```

#### Step 6: Add Configuration

```json
// File: appsettings.json
{
  "IEXCloud": {
    "ApiKey": "",
    "BaseUrl": "https://cloud.iexapis.com/stable",
    "Timeout": 10,
    "MaxRetries": 3,
    "Enabled": false,
    "RateLimit": {
      "RequestsPerMinute": 100,
      "RequestsPerDay": 50000
    }
  }
}
```

#### Step 7: Create Response Models

```csharp
// File: StockSensePro.Infrastructure/Models/IEXCloudQuoteResponse.cs
public class IEXCloudQuoteResponse
{
    [JsonPropertyName("symbol")]
    public string Symbol { get; set; } = string.Empty;
    
    [JsonPropertyName("latestPrice")]
    public decimal LatestPrice { get; set; }
    
    [JsonPropertyName("change")]
    public decimal Change { get; set; }
    
    [JsonPropertyName("changePercent")]
    public decimal ChangePercent { get; set; }
    
    // Add other fields as needed
}
```

#### Step 8: Implement Data Mapping

```csharp
private MarketData MapToMarketData(IEXCloudQuoteResponse response)
{
    return new MarketData
    {
        Symbol = response.Symbol,
        CurrentPrice = response.LatestPrice,
        Change = response.Change,
        ChangePercent = response.ChangePercent * 100, // IEX returns decimal, we want percentage
        Timestamp = DateTime.UtcNow,
        MarketState = DetermineMarketState(),
        // Map other fields...
    };
}
```

#### Step 9: Add Tests

```csharp
// File: StockSensePro.UnitTests/Services/IEXCloudServiceTests.cs
public class IEXCloudServiceTests
{
    [Fact]
    public async Task GetQuoteAsync_ValidSymbol_ReturnsMarketData()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When("https://cloud.iexapis.com/stable/stock/AAPL/quote*")
            .Respond("application/json", "{\"symbol\":\"AAPL\",\"latestPrice\":150.00}");
        
        var httpClient = mockHttp.ToHttpClient();
        var service = new IEXCloudService(httpClient, logger, settings, cacheService);
        
        // Act
        var result = await service.GetQuoteAsync("AAPL");
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal("AAPL", result.Symbol);
        Assert.Equal(150.00m, result.CurrentPrice);
    }
}
```

### Provider Implementation Checklist

- [ ] Add enum value to `DataProviderType`
- [ ] Create configuration class with `SectionName` constant
- [ ] Implement `IStockDataProvider` interface
- [ ] Add response model classes with JSON attributes
- [ ] Implement data mapping methods
- [ ] Add caching with provider-specific cache keys
- [ ] Implement error handling with custom exceptions
- [ ] Add structured logging for all operations
- [ ] Update `StockDataProviderFactory`
- [ ] Register in `Program.cs` with HttpClient and Polly policies
- [ ] Add configuration to `appsettings.json`
- [ ] Create unit tests for all methods
- [ ] Update documentation

---

## Testing Strategies

### Unit Testing Approach

#### 1. Testing Provider Services

Use `MockHttpMessageHandler` to simulate API responses:

```csharp
[Fact]
public async Task GetQuoteAsync_ValidSymbol_ReturnsMarketData()
{
    // Arrange
    var mockHttp = new MockHttpMessageHandler();
    mockHttp.When("*GLOBAL_QUOTE*")
        .Respond("application/json", @"{
            ""Global Quote"": {
                ""01. symbol"": ""AAPL"",
                ""05. price"": ""175.50"",
                ""09. change"": ""1.50"",
                ""10. change percent"": ""0.86%""
            }
        }");
    
    var httpClient = mockHttp.ToHttpClient();
    var service = new AlphaVantageService(httpClient, logger, settings, cacheSettings);
    
    // Act
    var result = await service.GetQuoteAsync("AAPL");
    
    // Assert
    Assert.NotNull(result);
    Assert.Equal("AAPL", result.Symbol);
    Assert.Equal(175.50m, result.CurrentPrice);
}
```

#### 2. Testing Error Scenarios

```csharp
[Fact]
public async Task GetQuoteAsync_InvalidApiKey_ThrowsInvalidApiKeyException()
{
    // Arrange
    var mockHttp = new MockHttpMessageHandler();
    mockHttp.When("*")
        .Respond("application/json", @"{""Error Message"": ""Invalid API key""}");
    
    var httpClient = mockHttp.ToHttpClient();
    var service = new AlphaVantageService(httpClient, logger, settings, cacheSettings);
    
    // Act & Assert
    await Assert.ThrowsAsync<InvalidApiKeyException>(() => service.GetQuoteAsync("AAPL"));
}

[Fact]
public async Task GetQuoteAsync_RateLimitExceeded_ThrowsRateLimitExceededException()
{
    // Arrange
    var mockHttp = new MockHttpMessageHandler();
    mockHttp.When("*")
        .Respond("application/json", @"{""Note"": ""Thank you for using Alpha Vantage! Our standard API call frequency is 5 calls per minute""}");
    
    var httpClient = mockHttp.ToHttpClient();
    var service = new AlphaVantageService(httpClient, logger, settings, cacheSettings);
    
    // Act & Assert
    await Assert.ThrowsAsync<RateLimitExceededException>(() => service.GetQuoteAsync("AAPL"));
}
```

#### 3. Testing Caching Behavior

```csharp
[Fact]
public async Task GetQuoteAsync_CacheHit_DoesNotCallApi()
{
    // Arrange
    var mockCache = new Mock<ICacheService>();
    var cachedData = new MarketData { Symbol = "AAPL", CurrentPrice = 175.50m };
    mockCache.Setup(c => c.GetAsync<MarketData>(It.IsAny<string>()))
        .ReturnsAsync(cachedData);
    
    var mockHttp = new MockHttpMessageHandler();
    // No HTTP setup - should not be called
    
    var httpClient = mockHttp.ToHttpClient();
    var service = new AlphaVantageService(httpClient, logger, settings, cacheSettings, mockCache.Object);
    
    // Act
    var result = await service.GetQuoteAsync("AAPL");
    
    // Assert
    Assert.Equal(cachedData, result);
    mockHttp.GetMatchCount(mockHttp.When("*")); // Should be 0
}

[Fact]
public async Task GetQuoteAsync_CacheMiss_CallsApiAndCaches()
{
    // Arrange
    var mockCache = new Mock<ICacheService>();
    mockCache.Setup(c => c.GetAsync<MarketData>(It.IsAny<string>()))
        .ReturnsAsync((MarketData?)null); // Cache miss
    
    var mockHttp = new MockHttpMessageHandler();
    mockHttp.When("*GLOBAL_QUOTE*")
        .Respond("application/json", @"{""Global Quote"": {""01. symbol"": ""AAPL"", ""05. price"": ""175.50""}}");
    
    var httpClient = mockHttp.ToHttpClient();
    var service = new AlphaVantageService(httpClient, logger, settings, cacheSettings, mockCache.Object);
    
    // Act
    var result = await service.GetQuoteAsync("AAPL");
    
    // Assert
    Assert.NotNull(result);
    mockCache.Verify(c => c.SetAsync(
        It.IsAny<string>(),
        It.IsAny<MarketData>(),
        It.IsAny<TimeSpan>()), Times.Once);
}
```

#### 4. Testing Provider Strategies

```csharp
[Fact]
public void SelectProvider_PrimaryHealthy_ReturnsPrimaryProvider()
{
    // Arrange
    var context = new DataProviderContext
    {
        Symbol = "AAPL",
        Operation = "GetQuote",
        ProviderHealth = new Dictionary<DataProviderType, ProviderHealth>
        {
            [DataProviderType.AlphaVantage] = new ProviderHealth { IsHealthy = true },
            [DataProviderType.YahooFinance] = new ProviderHealth { IsHealthy = true }
        }
    };
    
    var strategy = new FallbackProviderStrategy(factory, settings, logger);
    
    // Act
    var provider = strategy.SelectProvider(context);
    
    // Assert
    Assert.IsType<AlphaVantageService>(provider);
}

[Fact]
public void SelectProvider_PrimaryUnhealthy_ReturnsFallbackProvider()
{
    // Arrange
    var context = new DataProviderContext
    {
        Symbol = "AAPL",
        Operation = "GetQuote",
        ProviderHealth = new Dictionary<DataProviderType, ProviderHealth>
        {
            [DataProviderType.AlphaVantage] = new ProviderHealth { IsHealthy = false },
            [DataProviderType.YahooFinance] = new ProviderHealth { IsHealthy = true }
        }
    };
    
    var strategy = new FallbackProviderStrategy(factory, settings, logger);
    
    // Act
    var provider = strategy.SelectProvider(context);
    
    // Assert
    Assert.IsType<YahooFinanceService>(provider);
}
```

#### 5. Testing Rate Limiting

```csharp
[Fact]
public async Task TryAcquireAsync_WithinLimit_ReturnsTrue()
{
    // Arrange
    var settings = new AlphaVantageSettings
    {
        RateLimit = new RateLimitSettings
        {
            RequestsPerMinute = 5,
            RequestsPerDay = 25
        }
    };
    var rateLimiter = new AlphaVantageRateLimiter(Options.Create(settings), logger);
    
    // Act
    var result = await rateLimiter.TryAcquireAsync();
    
    // Assert
    Assert.True(result);
}

[Fact]
public async Task TryAcquireAsync_ExceedsMinuteLimit_ReturnsFalse()
{
    // Arrange
    var settings = new AlphaVantageSettings
    {
        RateLimit = new RateLimitSettings
        {
            RequestsPerMinute = 2,
            RequestsPerDay = 25
        }
    };
    var rateLimiter = new AlphaVantageRateLimiter(Options.Create(settings), logger);
    
    // Act - Acquire all tokens
    await rateLimiter.TryAcquireAsync();
    await rateLimiter.TryAcquireAsync();
    var result = await rateLimiter.TryAcquireAsync(); // Should fail
    
    // Assert
    Assert.False(result);
}
```

### Integration Testing

#### Testing with Real APIs

```csharp
[Fact]
[Trait("Category", "Integration")]
public async Task GetQuoteAsync_RealApi_ReturnsValidData()
{
    // Arrange
    var apiKey = Environment.GetEnvironmentVariable("ALPHA_VANTAGE_API_KEY");
    if (string.IsNullOrEmpty(apiKey))
    {
        // Skip test if API key not available
        return;
    }
    
    var settings = new AlphaVantageSettings { ApiKey = apiKey };
    var httpClient = new HttpClient();
    var service = new AlphaVantageService(httpClient, logger, Options.Create(settings), cacheSettings);
    
    // Act
    var result = await service.GetQuoteAsync("AAPL");
    
    // Assert
    Assert.NotNull(result);
    Assert.Equal("AAPL", result.Symbol);
    Assert.True(result.CurrentPrice > 0);
}
```

### Test Organization

```
tests/
├── StockSensePro.UnitTests/
│   ├── Services/
│   │   ├── AlphaVantageServiceTests.cs
│   │   ├── YahooFinanceServiceTests.cs
│   │   └── MockYahooFinanceServiceTests.cs
│   ├── Strategies/
│   │   ├── PrimaryProviderStrategyTests.cs
│   │   ├── FallbackProviderStrategyTests.cs
│   │   ├── RoundRobinProviderStrategyTests.cs
│   │   └── CostOptimizedProviderStrategyTests.cs
│   ├── RateLimiting/
│   │   └── AlphaVantageRateLimiterTests.cs
│   └── Factories/
│       └── StockDataProviderFactoryTests.cs
└── StockSensePro.IntegrationTests/
    ├── AlphaVantageIntegrationTests.cs
    ├── ProviderFallbackTests.cs
    └── EndToEndTests.cs
```

---

## Troubleshooting Guide

### Common Issues and Solutions

#### Issue 1: Provider Not Available

**Symptoms**:
```
System.ArgumentException: Unknown provider type: AlphaVantage
```

**Diagnosis**:
1. Check if provider is registered in `Program.cs`
2. Verify configuration section exists in `appsettings.json`
3. Ensure `Enabled` flag is set to `true`

**Solution**:
```csharp
// In Program.cs, ensure these lines exist:
builder.Services.AddScoped<AlphaVantageService>();
builder.Services.AddSingleton(alphaVantageSettings);

// In appsettings.json:
{
  "AlphaVantage": {
    "Enabled": true,
    "ApiKey": "your-key-here"
  }
}
```

#### Issue 2: Rate Limiter Not Working

**Symptoms**:
- API calls exceed configured rate limits
- No rate limit warnings in logs

**Diagnosis**:
1. Check if rate limiter is registered as singleton
2. Verify rate limiter is injected into service
3. Check rate limit configuration values

**Solution**:
```csharp
// In Program.cs:
builder.Services.AddSingleton<IAlphaVantageRateLimiter, AlphaVantageRateLimiter>();

// In AlphaVantageService constructor:
public AlphaVantageService(
    // ... other parameters
    IAlphaVantageRateLimiter? rateLimiter = null)
{
    _rateLimiter = rateLimiter;
}

// In ExecuteRequestWithLoggingAsync:
if (_rateLimiter != null)
{
    var acquired = await _rateLimiter.TryAcquireAsync(cancellationToken);
    if (!acquired)
    {
        throw new RateLimitExceededException(...);
    }
}
```

#### Issue 3: Cache Not Working

**Symptoms**:
- Every request hits the API
- Cache hit rate is 0%

**Diagnosis**:
1. Check Redis connection
2. Verify cache service is injected
3. Check cache key format
4. Verify TTL configuration

**Solution**:
```bash
# Test Redis connection
redis-cli ping
# Should return: PONG

# Check cache keys
redis-cli KEYS "alphavantage:*"

# Check TTL for a key
redis-cli TTL "alphavantage:quote:AAPL"
```

```csharp
// Verify cache service injection:
public AlphaVantageService(
    // ... other parameters
    ICacheService? cacheService = null)
{
    _cacheService = cacheService;
}

// Check cache usage:
if (_cacheService != null)
{
    var cached = await _cacheService.GetAsync<MarketData>(cacheKey);
    if (cached != null)
    {
        _logger.LogDebug("Cache hit for {Symbol}", symbol);
        return cached;
    }
}
```

#### Issue 4: Provider Fallback Not Triggering

**Symptoms**:
- Primary provider fails but fallback doesn't activate
- Requests fail instead of falling back

**Diagnosis**:
1. Check strategy configuration
2. Verify fallback provider is configured
3. Check health monitor status
4. Review exception types

**Solution**:
```json
// Ensure fallback is configured:
{
  "DataProvider": {
    "PrimaryProvider": "AlphaVantage",
    "FallbackProvider": "YahooFinance",
    "Strategy": "Fallback",
    "EnableAutomaticFallback": true
  }
}
```

```csharp
// Verify strategy is registered:
builder.Services.AddScoped<IDataProviderStrategy>(sp =>
{
    var settings = sp.GetRequiredService<DataProviderSettings>();
    return settings.Strategy switch
    {
        ProviderStrategyType.Fallback => sp.GetRequiredService<FallbackProviderStrategy>(),
        // ... other strategies
    };
});

// Check exception handling in StockService:
try
{
    var provider = _strategy.SelectProvider(context);
    return await provider.GetQuoteAsync(symbol, cancellationToken);
}
catch (ApiUnavailableException ex)
{
    _logger.LogWarning(ex, "Primary provider failed, trying fallback");
    var fallback = _strategy.GetFallbackProvider();
    return await fallback.GetQuoteAsync(symbol, cancellationToken);
}
```

#### Issue 5: Data Enrichment Not Working

**Symptoms**:
- Bid/ask prices are null
- 52-week high/low are null
- Average volume is null

**Diagnosis**:
1. Check enrichment configuration
2. Verify Yahoo Finance provider is available
3. Check for enrichment errors in logs
4. Verify calculated fields cache

**Solution**:
```json
// Enable enrichment in configuration:
{
  "AlphaVantage": {
    "DataEnrichment": {
      "EnableBidAskEnrichment": true,
      "EnableCalculated52WeekRange": true,
      "EnableCalculatedAverageVolume": true,
      "CalculatedFieldsCacheTTL": 86400
    }
  }
}
```

```csharp
// Verify Yahoo Finance provider is injected:
public AlphaVantageService(
    // ... other parameters
    IStockDataProvider? yahooFinanceProvider = null)
{
    _yahooFinanceProvider = yahooFinanceProvider;
}

// Check enrichment logs:
_logger.LogInformation("Enriching {Symbol} with Yahoo Finance bid/ask", symbol);
_logger.LogInformation("Calculating 52-week range for {Symbol}", symbol);
```

#### Issue 6: Health Monitoring Not Updating

**Symptoms**:
- Provider health status never changes
- Health checks not running

**Diagnosis**:
1. Check if health monitor is registered as singleton
2. Verify background task is running
3. Check health check interval configuration

**Solution**:
```csharp
// In Program.cs, ensure health monitor is singleton:
builder.Services.AddSingleton<IProviderHealthMonitor, ProviderHealthMonitor>();

// Verify background health monitoring task:
_ = Task.Run(async () =>
{
    while (true)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(settings.HealthCheckIntervalSeconds));
            
            foreach (var provider in availableProviders)
            {
                _ = healthMonitor.CheckHealthAsync(provider);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error in background health monitoring");
        }
    }
});
```

### Debugging Tips

#### Enable Detailed Logging

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "StockSensePro.Infrastructure.Services.AlphaVantageService": "Debug",
      "StockSensePro.Application.Strategies": "Debug",
      "StockSensePro.Infrastructure.RateLimiting": "Debug"
    }
  }
}
```

#### Use Diagnostic Endpoints

```bash
# Check provider health
curl http://localhost:5000/api/health

# Check detailed metrics
curl http://localhost:5000/api/health/metrics

# Response includes:
# - Provider health status
# - Rate limit remaining
# - Cache hit rates
# - API call counts
# - Cost tracking
```

#### Monitor Redis Cache

```bash
# Monitor Redis in real-time
redis-cli MONITOR

# Check cache statistics
redis-cli INFO stats

# List all Alpha Vantage cache keys
redis-cli KEYS "alphavantage:*"

# Get cache value
redis-cli GET "alphavantage:quote:AAPL"

# Check TTL
redis-cli TTL "alphavantage:quote:AAPL"
```

#### Analyze Logs

```bash
# Filter logs for Alpha Vantage
cat logs/stocksensepro-*.log | grep "AlphaVantage"

# Find rate limit events
cat logs/stocksensepro-*.log | grep "Rate limit"

# Find API errors
cat logs/stocksensepro-*.log | grep "ERROR" | grep "Alpha"

# Count API calls per provider
cat logs/stocksensepro-*.log | grep "API Request" | grep "AlphaVantage" | wc -l
```

---

## Architecture Deep Dive

### Provider Strategy Pattern

The system uses the Strategy pattern to decouple provider selection logic from the application layer.

#### Strategy Interface

```csharp
public interface IDataProviderStrategy
{
    IStockDataProvider SelectProvider(DataProviderContext context);
    IStockDataProvider GetFallbackProvider();
    string GetStrategyName();
}
```

#### Strategy Implementations

**1. PrimaryProviderStrategy**

Always uses the configured primary provider. Simplest strategy with no fallback.

```csharp
public class PrimaryProviderStrategy : ProviderStrategyBase
{
    public override IStockDataProvider SelectProvider(DataProviderContext context)
    {
        return _factory.CreateProvider(_settings.PrimaryProvider);
    }
    
    public override IStockDataProvider GetFallbackProvider()
    {
        // No fallback in primary strategy
        return _factory.CreateProvider(_settings.PrimaryProvider);
    }
}
```

**2. FallbackProviderStrategy**

Uses primary provider when healthy, falls back to secondary on failure.

```csharp
public class FallbackProviderStrategy : ProviderStrategyBase
{
    public override IStockDataProvider SelectProvider(DataProviderContext context)
    {
        var primaryHealth = context.ProviderHealth[_settings.PrimaryProvider];
        
        if (primaryHealth.IsHealthy)
        {
            return _factory.CreateProvider(_settings.PrimaryProvider);
        }
        
        _logger.LogWarning("Primary provider unhealthy, using fallback");
        return GetFallbackProvider();
    }
    
    public override IStockDataProvider GetFallbackProvider()
    {
        if (_settings.FallbackProvider.HasValue)
        {
            return _factory.CreateProvider(_settings.FallbackProvider.Value);
        }
        
        // Default to Yahoo Finance if no fallback configured
        return _factory.CreateProvider(DataProviderType.YahooFinance);
    }
}
```

**3. RoundRobinProviderStrategy**

Distributes load evenly across all healthy providers.

```csharp
public class RoundRobinProviderStrategy : ProviderStrategyBase
{
    private int _currentIndex = 0;
    private readonly object _lock = new object();
    
    public override IStockDataProvider SelectProvider(DataProviderContext context)
    {
        var healthyProviders = _factory.GetAvailableProviders()
            .Where(p => context.ProviderHealth[p].IsHealthy)
            .ToList();
        
        if (healthyProviders.Count == 0)
        {
            throw new InvalidOperationException("No healthy providers available");
        }
        
        lock (_lock)
        {
            var provider = healthyProviders[_currentIndex % healthyProviders.Count];
            _currentIndex++;
            return _factory.CreateProvider(provider);
        }
    }
}
```

**4. CostOptimizedProviderStrategy**

Prefers cheaper providers when rate limits allow.

```csharp
public class CostOptimizedProviderStrategy : ProviderStrategyBase
{
    public override IStockDataProvider SelectProvider(DataProviderContext context)
    {
        // Check free providers first (Yahoo Finance)
        if (context.ProviderHealth[DataProviderType.YahooFinance].IsHealthy)
        {
            return _factory.CreateProvider(DataProviderType.YahooFinance);
        }
        
        // Check paid providers with remaining quota
        if (context.RateLimitRemaining[DataProviderType.AlphaVantage] > 0 &&
            context.ProviderHealth[DataProviderType.AlphaVantage].IsHealthy)
        {
            return _factory.CreateProvider(DataProviderType.AlphaVantage);
        }
        
        // Fall back to any available provider
        return GetFallbackProvider();
    }
}
```

### Rate Limiting Architecture

#### Token Bucket Algorithm

The rate limiter uses a token bucket algorithm to enforce both minute and daily limits:

```csharp
public class AlphaVantageRateLimiter : IAlphaVantageRateLimiter
{
    private readonly SemaphoreSlim _minuteSemaphore;
    private readonly SemaphoreSlim _daySemaphore;
    private readonly Timer _minuteResetTimer;
    private readonly Timer _dayResetTimer;
    
    public AlphaVantageRateLimiter(IOptions<AlphaVantageSettings> settings, ILogger<AlphaVantageRateLimiter> logger)
    {
        var rateLimit = settings.Value.RateLimit;
        
        // Initialize semaphores with max tokens
        _minuteSemaphore = new SemaphoreSlim(rateLimit.RequestsPerMinute, rateLimit.RequestsPerMinute);
        _daySemaphore = new SemaphoreSlim(rateLimit.RequestsPerDay, rateLimit.RequestsPerDay);
        
        // Set up automatic token refill
        _minuteResetTimer = new Timer(_ => RefillMinuteTokens(), null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
        _dayResetTimer = new Timer(_ => RefillDayTokens(), null, TimeSpan.FromDays(1), TimeSpan.FromDays(1));
    }
    
    public async Task<bool> TryAcquireAsync(CancellationToken cancellationToken = default)
    {
        // Try to acquire both minute and day tokens
        var minuteAcquired = await _minuteSemaphore.WaitAsync(0, cancellationToken);
        if (!minuteAcquired)
        {
            return false; // Minute limit exceeded
        }
        
        var dayAcquired = await _daySemaphore.WaitAsync(0, cancellationToken);
        if (!dayAcquired)
        {
            _minuteSemaphore.Release(); // Release minute token
            return false; // Day limit exceeded
        }
        
        return true;
    }
    
    private void RefillMinuteTokens()
    {
        // Release all tokens back to the pool
        var currentCount = _minuteSemaphore.CurrentCount;
        var tokensToAdd = _settings.RateLimit.RequestsPerMinute - currentCount;
        
        if (tokensToAdd > 0)
        {
            _minuteSemaphore.Release(tokensToAdd);
        }
    }
}
```

#### Rate Limit Status

```csharp
public class RateLimitStatus
{
    public int MinuteRequestsRemaining { get; set; }
    public int MinuteRequestsLimit { get; set; }
    public TimeSpan MinuteWindowResetIn { get; set; }
    
    public int DayRequestsRemaining { get; set; }
    public int DayRequestsLimit { get; set; }
    public TimeSpan DayWindowResetIn { get; set; }
}
```

### Health Monitoring Architecture

#### Health Check Flow

```
┌─────────────────────────────────────────────────────────────┐
│                Background Health Monitor                     │
│  (Runs every HealthCheckIntervalSeconds)                    │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│              For Each Provider                               │
│  1. Call provider.IsHealthyAsync()                          │
│  2. Measure response time                                    │
│  3. Update health status                                     │
│  4. Track consecutive failures                               │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│              Health Status Storage                           │
│  Dictionary<DataProviderType, ProviderHealth>               │
└─────────────────────────────────────────────────────────────┘
```

#### Provider Health Model

```csharp
public class ProviderHealth
{
    public bool IsHealthy { get; set; }
    public DateTime LastChecked { get; set; }
    public int ConsecutiveFailures { get; set; }
    public TimeSpan AverageResponseTime { get; set; }
    public string? LastError { get; set; }
}
```

#### Health Monitor Implementation

```csharp
public class ProviderHealthMonitor : IProviderHealthMonitor
{
    private readonly Dictionary<DataProviderType, ProviderHealth> _healthStatus;
    private readonly IStockDataProviderFactory _factory;
    private readonly ILogger<ProviderHealthMonitor> _logger;
    
    public async Task CheckHealthAsync(DataProviderType provider)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var providerInstance = _factory.CreateProvider(provider);
            var isHealthy = await providerInstance.IsHealthyAsync();
            stopwatch.Stop();
            
            _healthStatus[provider] = new ProviderHealth
            {
                IsHealthy = isHealthy,
                LastChecked = DateTime.UtcNow,
                ConsecutiveFailures = isHealthy ? 0 : _healthStatus[provider].ConsecutiveFailures + 1,
                AverageResponseTime = stopwatch.Elapsed,
                LastError = null
            };
            
            _logger.LogInformation(
                "Health check for {Provider}: {Status}, ResponseTime: {ResponseTime}ms",
                provider,
                isHealthy ? "Healthy" : "Unhealthy",
                stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            _healthStatus[provider] = new ProviderHealth
            {
                IsHealthy = false,
                LastChecked = DateTime.UtcNow,
                ConsecutiveFailures = _healthStatus[provider].ConsecutiveFailures + 1,
                AverageResponseTime = stopwatch.Elapsed,
                LastError = ex.Message
            };
            
            _logger.LogError(ex, "Health check failed for {Provider}", provider);
        }
    }
    
    public ProviderHealth GetHealth(DataProviderType provider)
    {
        return _healthStatus.TryGetValue(provider, out var health)
            ? health
            : new ProviderHealth { IsHealthy = false, LastChecked = DateTime.MinValue };
    }
}
```

### Cost Tracking Architecture

#### Cost Calculator

```csharp
public interface IProviderCostCalculator
{
    decimal CalculateCost(DataProviderType provider, int requestCount);
    decimal GetCostPerRequest(DataProviderType provider);
}

public class ProviderCostCalculator : IProviderCostCalculator
{
    private readonly ProviderCostSettings _settings;
    
    public decimal CalculateCost(DataProviderType provider, int requestCount)
    {
        var costPerRequest = GetCostPerRequest(provider);
        return costPerRequest * requestCount;
    }
    
    public decimal GetCostPerRequest(DataProviderType provider)
    {
        return provider switch
        {
            DataProviderType.YahooFinance => 0m, // Free
            DataProviderType.AlphaVantage => _settings.AlphaVantageCostPerRequest,
            DataProviderType.Mock => 0m, // Free
            _ => 0m
        };
    }
}
```

#### Cost Tracker

```csharp
public interface IProviderCostTracker
{
    void TrackRequest(DataProviderType provider);
    ProviderCostSummary GetCostSummary(DataProviderType provider);
    Dictionary<DataProviderType, ProviderCostSummary> GetAllCostSummaries();
}

public class ProviderCostTracker : IProviderCostTracker
{
    private readonly Dictionary<DataProviderType, ProviderCostData> _costData;
    private readonly IProviderCostCalculator _calculator;
    
    public void TrackRequest(DataProviderType provider)
    {
        lock (_costData)
        {
            if (!_costData.ContainsKey(provider))
            {
                _costData[provider] = new ProviderCostData();
            }
            
            _costData[provider].RequestsToday++;
            _costData[provider].RequestsThisMonth++;
            _costData[provider].TotalRequests++;
        }
    }
    
    public ProviderCostSummary GetCostSummary(DataProviderType provider)
    {
        var data = _costData.GetValueOrDefault(provider, new ProviderCostData());
        
        return new ProviderCostSummary
        {
            Provider = provider,
            RequestsToday = data.RequestsToday,
            RequestsThisMonth = data.RequestsThisMonth,
            EstimatedCostToday = _calculator.CalculateCost(provider, data.RequestsToday),
            EstimatedCostThisMonth = _calculator.CalculateCost(provider, data.RequestsThisMonth),
            CostPerRequest = _calculator.GetCostPerRequest(provider)
        };
    }
}
```

---

## Best Practices

### 1. Configuration Management

**DO**:
- Use `IOptions<T>` pattern for configuration
- Validate configuration on startup
- Use User Secrets for development
- Use Environment Variables or Key Vault for production
- Provide sensible defaults

**DON'T**:
- Hard-code API keys
- Commit secrets to source control
- Use production keys in development
- Skip configuration validation

### 2. Logging

**DO**:
- Use structured logging with named parameters
- Log all API requests and responses
- Log rate limit events
- Log provider selection decisions
- Include correlation IDs for tracing

**DON'T**:
- Log API keys or sensitive data
- Log excessive details in production
- Use string interpolation in log messages
- Ignore log levels

### 3. Error Handling

**DO**:
- Use custom exceptions for specific error types
- Implement retry logic with exponential backoff
- Return cached data on rate limit errors
- Trigger fallback on API unavailability
- Log errors with full context

**DON'T**:
- Swallow exceptions silently
- Retry indefinitely
- Expose internal errors to clients
- Fail fast without attempting recovery

### 4. Caching

**DO**:
- Cache aggressively to minimize API calls
- Use provider-specific cache keys
- Configure appropriate TTLs per data type
- Return stale cache on errors
- Monitor cache hit rates

**DON'T**:
- Cache error responses
- Use overly long TTLs for real-time data
- Ignore cache failures
- Cache sensitive data without encryption

### 5. Rate Limiting

**DO**:
- Enforce rate limits before making API calls
- Use token bucket algorithm for smooth rate limiting
- Track both minute and daily limits
- Queue requests when rate limited
- Monitor rate limit usage

**DON'T**:
- Make API calls without checking limits
- Ignore rate limit responses from API
- Use fixed delays instead of token buckets
- Fail requests immediately on rate limit

### 6. Testing

**DO**:
- Write unit tests for all provider methods
- Mock HTTP responses for predictable tests
- Test error scenarios thoroughly
- Use integration tests with real APIs (sparingly)
- Test provider switching and fallback

**DON'T**:
- Test only happy paths
- Make real API calls in unit tests
- Skip testing error handling
- Ignore edge cases
- Test with production API keys

### 7. Performance

**DO**:
- Use async/await throughout
- Implement connection pooling (HttpClient)
- Use Polly for resilience policies
- Batch requests when possible
- Monitor response times

**DON'T**:
- Block on async operations
- Create new HttpClient instances
- Make synchronous API calls
- Ignore timeouts
- Skip performance monitoring

### 8. Security

**DO**:
- Store API keys securely
- Use HTTPS for all API calls
- Validate all inputs
- Sanitize error messages
- Implement rate limiting

**DON'T**:
- Log API keys
- Expose API keys in URLs
- Trust user input
- Return detailed errors to clients
- Skip input validation

---

## Quick Reference

### Configuration Sections

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
    },
    "DataEnrichment": {
      "EnableBidAskEnrichment": true,
      "EnableCalculated52WeekRange": true,
      "EnableCalculatedAverageVolume": true,
      "CalculatedFieldsCacheTTL": 86400
    }
  },
  "Cache": {
    "AlphaVantage": {
      "QuoteTTL": 900,
      "HistoricalTTL": 86400,
      "FundamentalsTTL": 21600,
      "ProfileTTL": 604800,
      "SearchTTL": 3600
    }
  }
}
```

### Key Interfaces

- `IStockDataProvider`: Provider contract
- `IDataProviderStrategy`: Strategy for provider selection
- `IStockDataProviderFactory`: Factory for creating providers
- `IProviderHealthMonitor`: Health monitoring
- `IAlphaVantageRateLimiter`: Rate limiting
- `IProviderCostTracker`: Cost tracking
- `ICacheService`: Caching abstraction

### Custom Exceptions

- `InvalidApiKeyException`: Authentication failure
- `RateLimitExceededException`: Rate limit exceeded
- `SymbolNotFoundException`: Invalid symbol
- `ApiUnavailableException`: API error or unavailable

### Useful Commands

```bash
# Set API key (development)
dotnet user-secrets set "AlphaVantage:ApiKey" "YOUR_KEY"

# Run tests
dotnet test

# Check health
curl http://localhost:5000/api/health

# Check metrics
curl http://localhost:5000/api/health/metrics

# Monitor Redis
redis-cli MONITOR

# View logs
tail -f logs/stocksensepro-*.log
```

---

## Additional Resources

### Documentation

- [User Guide](./ALPHA_VANTAGE_USER_GUIDE.md) - Configuration and usage
- [API Key Security](./API_KEY_SECURITY.md) - Secure key management
- [Provider Caching](./PROVIDER_CACHING.md) - Caching strategies
- [Metrics Endpoint](./PROVIDER_METRICS_ENDPOINT.md) - Monitoring and metrics

### External Resources

- [Alpha Vantage API Documentation](https://www.alphavantage.co/documentation/)
- [Alpha Vantage Support](https://www.alphavantage.co/support/)
- [Polly Documentation](https://github.com/App-vNext/Polly)
- [.NET Options Pattern](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options)

---

**Last Updated**: November 22, 2025  
**Version**: 1.0  
**Maintainer**: StockSensePro Development Team

