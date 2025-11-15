# Testing Challenges and Recommendations

## Current Testing Limitations

### 1. YahooFinanceService Testing Challenges

**Problem**: The `YahooFinanceService` class has design issues that make unit testing difficult:

1. **Internal HttpClient Creation**: The service creates a second `HttpClient` (`_quoteHttpClient`) in the constructor:
   ```csharp
   _quoteHttpClient = new HttpClient
   {
       BaseAddress = new Uri("https://query1.finance.yahoo.com/v7/finance/quote/")
   };
   ```
   This cannot be mocked in unit tests.

2. **Polly Policy Integration**: The HttpClient is wrapped with Polly policies in `Program.cs`, which interfere with unit testing when we try to test error scenarios.

3. **Multiple Base URLs**: The service uses different base URLs for different endpoints, making it harder to mock consistently.

**Impact**:
- Cannot properly unit test the service in isolation
- Error handling tests fail because Polly retries interfere
- Mock HttpClient responses don't work for all endpoints

**Recommended Solutions**:

#### Option 1: Refactor for Testability (Recommended)
Refactor `YahooFinanceService` to accept an `IHttpClientFactory`:

```csharp
public class YahooFinanceService : IYahooFinanceService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<YahooFinanceService> _logger;

    public YahooFinanceService(IHttpClientFactory httpClientFactory, ILogger<YahooFinanceService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<MarketData> GetQuoteAsync(string symbol, CancellationToken cancellationToken = default)
    {
        var httpClient = _httpClientFactory.CreateClient("YahooFinanceQuote");
        // ... rest of implementation
    }
}
```

Then configure named clients in `Program.cs`:
```csharp
builder.Services.AddHttpClient("YahooFinanceChart", client =>
{
    client.BaseAddress = new Uri("https://query1.finance.yahoo.com/v8/finance/chart/");
})
.AddPolicyHandler(GetRetryPolicy())
.AddPolicyHandler(GetCircuitBreakerPolicy());

builder.Services.AddHttpClient("YahooFinanceQuote", client =>
{
    client.BaseAddress = new Uri("https://query1.finance.yahoo.com/v7/finance/quote/");
})
.AddPolicyHandler(GetRetryPolicy())
.AddPolicyHandler(GetCircuitBreakerPolicy());
```

**Benefits**:
- Fully testable with mocked IHttpClientFactory
- Polly policies can be excluded in tests
- Better separation of concerns
- Follows .NET best practices

#### Option 2: Integration Tests Only
Skip unit tests for YahooFinanceService and rely on integration tests:

```csharp
[Collection("Integration")]
public class YahooFinanceIntegrationTests
{
    [Fact(Skip = "Integration test - requires real API")]
    public async Task GetQuoteAsync_WithRealAPI_ReturnsData()
    {
        // Test against real Yahoo Finance API
        // Use rate limiting to avoid hitting limits
    }
}
```

**Benefits**:
- Tests real behavior
- No mocking complexity
- Validates actual API integration

**Drawbacks**:
- Slower tests
- Requires network access
- Subject to API rate limits
- Can't test error scenarios easily

#### Option 3: Wrapper Pattern
Create a thin wrapper around HttpClient that can be mocked:

```csharp
public interface IHttpClientWrapper
{
    Task<HttpResponseMessage> GetAsync(string requestUri, CancellationToken cancellationToken);
}

public class HttpClientWrapper : IHttpClientWrapper
{
    private readonly HttpClient _httpClient;
    
    public HttpClientWrapper(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
    
    public Task<HttpResponseMessage> GetAsync(string requestUri, CancellationToken cancellationToken)
    {
        return _httpClient.GetAsync(requestUri, cancellationToken);
    }
}
```

**Benefits**:
- Minimal refactoring
- Testable

**Drawbacks**:
- Adds abstraction layer
- Still need to handle multiple clients

---

## Current Test Status

### What We Can Test Now

1. **StockService (Application Layer)** ✅
   - Fully testable with mocked dependencies
   - 14 tests covering caching logic
   - All tests passing

2. **Cache Service** ✅
   - Can be tested with mocked Redis connection
   - Tests for Get/Set/Remove/Exists operations

3. **Domain Models** ✅
   - Simple DTOs, tested indirectly through service tests

### What We Cannot Test Easily

1. **YahooFinanceService** ❌
   - Cannot mock internal HttpClient
   - Polly policies interfere with error testing
   - Response parsing logic not isolated

2. **Polly Policies** ❌
   - Retry behavior
   - Circuit breaker behavior
   - Timeout enforcement

3. **End-to-End Flows** ❌
   - Full request/response cycle
   - Real API integration
   - Error propagation through layers

---

## Immediate Recommendations

### Short Term (Current Sprint)
1. ✅ Complete StockService tests (DONE)
2. ❌ Document testing limitations (THIS DOCUMENT)
3. ❌ Add RedisCacheService unit tests
4. ❌ Add simple integration tests for happy path scenarios

### Medium Term (Next Sprint)
1. ❌ Refactor YahooFinanceService to use IHttpClientFactory
2. ❌ Add comprehensive unit tests for refactored service
3. ❌ Add Polly policy tests
4. ❌ Add error handling tests

### Long Term (Future)
1. ❌ Add comprehensive integration test suite
2. ❌ Add performance tests
3. ❌ Set up test coverage reporting
4. ❌ Add contract tests for Yahoo Finance API

---

## Alternative: Focus on What Matters

Given the testing challenges, we can adopt a pragmatic approach:

### Priority 1: Test Business Logic ✅
- StockService caching logic (DONE)
- Data transformation logic
- Validation logic

### Priority 2: Integration Tests
- Test real API calls (with rate limiting)
- Test end-to-end flows
- Test error scenarios with real responses

### Priority 3: Refactor for Testability
- Only refactor if testing becomes critical
- Focus on high-risk areas first
- Balance test coverage with development velocity

---

## Conclusion

**Current State**:
- Application layer (StockService) is well-tested ✅
- Infrastructure layer (YahooFinanceService) has design issues that prevent effective unit testing ❌
- Integration tests would provide more value than forcing unit tests on poorly designed code

**Recommendation**:
1. Accept current test coverage for MVP
2. Add integration tests for critical paths
3. Plan refactoring for next sprint
4. Focus development effort on new features rather than fighting with untestable code

**Risk Assessment**:
- 🟡 Medium Risk: YahooFinanceService parsing logic not tested
- 🟢 Low Risk: StockService well-tested, business logic protected
- 🟡 Medium Risk: Error handling not fully validated

The pragmatic approach is to ship with current test coverage and improve in future iterations.
