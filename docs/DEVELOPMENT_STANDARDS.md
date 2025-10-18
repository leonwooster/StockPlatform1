# StockSense Pro Development Standards

## Project Overview
StockSense Pro is a comprehensive stock analysis platform with Vue 3 frontend and C# backend, integrating Yahoo Finance API and AI-powered trading agents.

## Architecture Principles

### SOLID Principles

#### 1. Single Responsibility Principle (SRP)
- Each class/component should have one reason to change
- Services should handle one specific domain (e.g., MarketDataService, SentimentAnalysisService)
- Vue components should focus on a single UI concern
- Example:
  ```csharp
  // ✅ Good - Single responsibility
  public class StockPriceService
  {
      public Task<StockPrice> GetCurrentPrice(string symbol);
      public Task<List<StockPrice>> GetHistoricalPrices(string symbol, DateRange range);
  }
  
  // ❌ Bad - Multiple responsibilities
  public class StockService
  {
      public Task<StockPrice> GetPrice(string symbol);
      public Task SendEmail(string to, string subject);
      public Task LogToDatabase(string message);
  }
  ```

#### 2. Open/Closed Principle (OCP)
- Open for extension, closed for modification
- Use interfaces and abstract classes
- Example:
  ```csharp
  // ✅ Good - Extensible without modification
  public interface IDataProvider
  {
      Task<MarketData> FetchData(string symbol);
  }
  
  public class YahooFinanceProvider : IDataProvider { }
  public class PolygonProvider : IDataProvider { }
  public class AlphaVantageProvider : IDataProvider { }
  ```

#### 3. Liskov Substitution Principle (LSP)
- Derived classes must be substitutable for base classes
- Maintain behavioral consistency
- Example:
  ```csharp
  // ✅ Good - Substitutable
  public abstract class TechnicalIndicator
  {
      public abstract double Calculate(List<double> prices);
  }
  
  public class RSI : TechnicalIndicator
  {
      public override double Calculate(List<double> prices)
      {
          // RSI calculation
      }
  }
  ```

#### 4. Interface Segregation Principle (ISP)
- Clients should not depend on interfaces they don't use
- Create specific interfaces
- Example:
  ```csharp
  // ✅ Good - Segregated interfaces
  public interface IReadableRepository<T>
  {
      Task<T> GetById(int id);
      Task<List<T>> GetAll();
  }
  
  public interface IWritableRepository<T>
  {
      Task<T> Create(T entity);
      Task Update(T entity);
      Task Delete(int id);
  }
  
  // ❌ Bad - Fat interface
  public interface IRepository<T>
  {
      Task<T> GetById(int id);
      Task<List<T>> GetAll();
      Task<T> Create(T entity);
      Task Update(T entity);
      Task Delete(int id);
      Task BulkInsert(List<T> entities);
      Task ExecuteStoredProcedure(string name);
  }
  ```

#### 5. Dependency Inversion Principle (DIP)
- Depend on abstractions, not concretions
- Use dependency injection
- Example:
  ```csharp
  // ✅ Good - Depends on abstraction
  public class StockAnalysisService
  {
      private readonly IDataProvider _dataProvider;
      private readonly ILogger<StockAnalysisService> _logger;
      
      public StockAnalysisService(IDataProvider dataProvider, ILogger<StockAnalysisService> logger)
      {
          _dataProvider = dataProvider;
          _logger = logger;
      }
  }
  ```

## Design Patterns (from refactoring.guru)

### Creational Patterns

#### 1. Factory Pattern
Use for creating data providers, indicators, or agents.
```csharp
public interface IDataProviderFactory
{
    IDataProvider CreateProvider(DataProviderType type);
}

public class DataProviderFactory : IDataProviderFactory
{
    public IDataProvider CreateProvider(DataProviderType type)
    {
        return type switch
        {
            DataProviderType.YahooFinance => new YahooFinanceProvider(),
            DataProviderType.Polygon => new PolygonProvider(),
            DataProviderType.AlphaVantage => new AlphaVantageProvider(),
            _ => throw new ArgumentException("Invalid provider type")
        };
    }
}
```

#### 2. Builder Pattern
Use for complex object construction (e.g., trading strategies, queries).
```csharp
public class StockQueryBuilder
{
    private string _symbol;
    private DateTime? _startDate;
    private DateTime? _endDate;
    private List<string> _indicators = new();
    
    public StockQueryBuilder WithSymbol(string symbol)
    {
        _symbol = symbol;
        return this;
    }
    
    public StockQueryBuilder WithDateRange(DateTime start, DateTime end)
    {
        _startDate = start;
        _endDate = end;
        return this;
    }
    
    public StockQueryBuilder AddIndicator(string indicator)
    {
        _indicators.Add(indicator);
        return this;
    }
    
    public StockQuery Build()
    {
        return new StockQuery(_symbol, _startDate, _endDate, _indicators);
    }
}
```

#### 3. Singleton Pattern
Use for configuration, logging, or cache managers.
```csharp
public sealed class CacheManager
{
    private static readonly Lazy<CacheManager> _instance = new(() => new CacheManager());
    private readonly IMemoryCache _cache;
    
    private CacheManager()
    {
        _cache = new MemoryCache(new MemoryCacheOptions());
    }
    
    public static CacheManager Instance => _instance.Value;
}
```

### Structural Patterns

#### 1. Adapter Pattern
Use for integrating different API providers.
```csharp
public interface IStandardizedMarketData
{
    string Symbol { get; }
    decimal Price { get; }
    DateTime Timestamp { get; }
}

public class YahooFinanceAdapter : IStandardizedMarketData
{
    private readonly YahooFinanceResponse _response;
    
    public YahooFinanceAdapter(YahooFinanceResponse response)
    {
        _response = response;
    }
    
    public string Symbol => _response.Ticker;
    public decimal Price => _response.RegularMarketPrice;
    public DateTime Timestamp => _response.RegularMarketTime;
}
```

#### 2. Decorator Pattern
Use for adding features to services (caching, logging, retry logic).
```csharp
public class CachedDataProvider : IDataProvider
{
    private readonly IDataProvider _innerProvider;
    private readonly ICacheService _cache;
    
    public CachedDataProvider(IDataProvider innerProvider, ICacheService cache)
    {
        _innerProvider = innerProvider;
        _cache = cache;
    }
    
    public async Task<MarketData> FetchData(string symbol)
    {
        var cacheKey = $"market_data_{symbol}";
        var cached = await _cache.GetAsync<MarketData>(cacheKey);
        
        if (cached != null)
            return cached;
        
        var data = await _innerProvider.FetchData(symbol);
        await _cache.SetAsync(cacheKey, data, TimeSpan.FromMinutes(15));
        
        return data;
    }
}
```

#### 3. Facade Pattern
Use for simplifying complex subsystems.
```csharp
public class TradingAnalysisFacade
{
    private readonly IMarketDataService _marketData;
    private readonly ITechnicalAnalysisService _technical;
    private readonly ISentimentAnalysisService _sentiment;
    private readonly IFundamentalAnalysisService _fundamental;
    
    public async Task<ComprehensiveAnalysis> AnalyzeStock(string symbol)
    {
        var marketData = await _marketData.GetData(symbol);
        var technical = await _technical.Analyze(marketData);
        var sentiment = await _sentiment.Analyze(symbol);
        var fundamental = await _fundamental.Analyze(symbol);
        
        return new ComprehensiveAnalysis(technical, sentiment, fundamental);
    }
}
```

### Behavioral Patterns

#### 1. Strategy Pattern
Use for different trading strategies or analysis algorithms.
```csharp
public interface ITradingStrategy
{
    Task<TradeSignal> GenerateSignal(MarketData data);
}

public class MomentumStrategy : ITradingStrategy
{
    public async Task<TradeSignal> GenerateSignal(MarketData data)
    {
        // Momentum-based logic
    }
}

public class ValueStrategy : ITradingStrategy
{
    public async Task<TradeSignal> GenerateSignal(MarketData data)
    {
        // Value-based logic
    }
}

public class TradingContext
{
    private ITradingStrategy _strategy;
    
    public void SetStrategy(ITradingStrategy strategy)
    {
        _strategy = strategy;
    }
    
    public async Task<TradeSignal> ExecuteStrategy(MarketData data)
    {
        return await _strategy.GenerateSignal(data);
    }
}
```

#### 2. Observer Pattern
Use for real-time data updates and event notifications.
```csharp
public interface IStockPriceObserver
{
    Task OnPriceUpdate(string symbol, decimal newPrice);
}

public class StockPriceSubject
{
    private readonly List<IStockPriceObserver> _observers = new();
    
    public void Attach(IStockPriceObserver observer)
    {
        _observers.Add(observer);
    }
    
    public void Detach(IStockPriceObserver observer)
    {
        _observers.Remove(observer);
    }
    
    public async Task NotifyPriceChange(string symbol, decimal newPrice)
    {
        foreach (var observer in _observers)
        {
            await observer.OnPriceUpdate(symbol, newPrice);
        }
    }
}
```

#### 3. Chain of Responsibility Pattern
Use for request processing pipelines (validation, authentication, rate limiting).
```csharp
public abstract class RequestHandler
{
    protected RequestHandler _nextHandler;
    
    public RequestHandler SetNext(RequestHandler handler)
    {
        _nextHandler = handler;
        return handler;
    }
    
    public abstract Task<Response> Handle(Request request);
}

public class RateLimitHandler : RequestHandler
{
    public override async Task<Response> Handle(Request request)
    {
        if (!await CheckRateLimit(request))
            return new Response { Error = "Rate limit exceeded" };
        
        return _nextHandler != null 
            ? await _nextHandler.Handle(request) 
            : new Response { Success = true };
    }
}

public class AuthenticationHandler : RequestHandler
{
    public override async Task<Response> Handle(Request request)
    {
        if (!await ValidateToken(request))
            return new Response { Error = "Unauthorized" };
        
        return _nextHandler != null 
            ? await _nextHandler.Handle(request) 
            : new Response { Success = true };
    }
}
```

## Code Organization

### Backend (C# .NET)

```
StockSensePro.Backend/
├── src/
│   ├── StockSensePro.API/                    # Web API project
│   │   ├── Controllers/
│   │   ├── Middleware/
│   │   ├── Filters/
│   │   └── Program.cs
│   ├── StockSensePro.Core/                   # Domain models and interfaces
│   │   ├── Entities/
│   │   ├── Interfaces/
│   │   ├── Enums/
│   │   └── ValueObjects/
│   ├── StockSensePro.Application/            # Business logic
│   │   ├── Services/
│   │   ├── DTOs/
│   │   ├── Mappings/
│   │   └── Validators/
│   ├── StockSensePro.Infrastructure/         # External concerns
│   │   ├── DataProviders/
│   │   │   ├── YahooFinance/
│   │   │   ├── Polygon/
│   │   │   └── AlphaVantage/
│   │   ├── Persistence/
│   │   │   ├── Repositories/
│   │   │   └── Configurations/
│   │   ├── Caching/
│   │   └── Logging/
│   └── StockSensePro.AI/                     # AI/ML services
│       ├── Agents/
│       ├── Models/
│       ├── Strategies/
│       └── Analysis/
├── tests/
│   ├── StockSensePro.UnitTests/
│   ├── StockSensePro.IntegrationTests/
│   └── StockSensePro.PerformanceTests/
└── docs/
    └── epics/
```

### Frontend (Vue 3)

```
StockSensePro.Frontend/
├── src/
│   ├── assets/                               # Static assets
│   ├── components/                           # Reusable components
│   │   ├── common/                          # Generic components
│   │   ├── charts/                          # Chart components
│   │   └── forms/                           # Form components
│   ├── composables/                         # Vue 3 composition functions
│   ├── layouts/                             # Layout components
│   ├── pages/                               # Page components
│   │   ├── Dashboard/
│   │   ├── TechnicalAnalysis/
│   │   ├── Screener/
│   │   ├── News/
│   │   └── AIAgents/
│   ├── router/                              # Vue Router configuration
│   ├── stores/                              # Pinia stores
│   │   ├── market.ts
│   │   ├── portfolio.ts
│   │   └── user.ts
│   ├── services/                            # API services
│   │   ├── api/
│   │   └── websocket/
│   ├── types/                               # TypeScript types
│   ├── utils/                               # Utility functions
│   └── App.vue
├── tests/
│   ├── unit/
│   └── e2e/
└── docs/
```

## Naming Conventions

### C# Backend

```csharp
// Interfaces: I prefix
public interface IStockDataService { }

// Classes: PascalCase
public class StockDataService { }

// Methods: PascalCase, verb-based
public async Task<Stock> GetStockBySymbol(string symbol) { }

// Private fields: _camelCase
private readonly ILogger _logger;

// Properties: PascalCase
public string Symbol { get; set; }

// Constants: PascalCase
public const int MaxRetryAttempts = 3;

// Async methods: suffix with Async
public async Task<Data> FetchDataAsync() { }
```

### Vue 3 / TypeScript

```typescript
// Components: PascalCase
export default defineComponent({
  name: 'StockChart'
})

// Composables: camelCase with 'use' prefix
export function useStockData() { }

// Variables/Functions: camelCase
const stockPrice = ref(0)
function calculateReturn() { }

// Constants: UPPER_SNAKE_CASE
const API_BASE_URL = 'https://api.example.com'

// Types/Interfaces: PascalCase
interface StockData { }
type TradeSignal = 'BUY' | 'SELL' | 'HOLD'

// Files: kebab-case
// stock-chart.vue
// use-stock-data.ts
```

## Logging Standards

### Structured Logging with Serilog

```csharp
// appsettings.json
{
  "Serilog": {
    "Using": ["Serilog.Sinks.Console", "Serilog.Sinks.File"],
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "outputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}"
        }
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/stocksensepro-.log",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 30,
          "outputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}"
        }
      }
    ],
    "Enrich": ["FromLogContext", "WithMachineName", "WithThreadId"]
  }
}

// Usage in code
public class StockDataService
{
    private readonly ILogger<StockDataService> _logger;
    
    public async Task<Stock> GetStock(string symbol)
    {
        _logger.LogInformation("Fetching stock data for {Symbol}", symbol);
        
        try
        {
            var stock = await _repository.GetBySymbol(symbol);
            
            _logger.LogInformation(
                "Successfully fetched stock data for {Symbol}. Price: {Price}", 
                symbol, 
                stock.Price
            );
            
            return stock;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex, 
                "Error fetching stock data for {Symbol}", 
                symbol
            );
            throw;
        }
    }
}
```

### Log Levels

- **Trace**: Very detailed logs, typically only enabled in development
- **Debug**: Debugging information, less detailed than Trace
- **Information**: General informational messages (API calls, business operations)
- **Warning**: Potentially harmful situations (rate limits approaching, retries)
- **Error**: Error events that might still allow the application to continue
- **Critical**: Critical failures that require immediate attention

### Log Categories

```csharp
// Performance logging
_logger.LogInformation(
    "API call completed. Endpoint: {Endpoint}, Duration: {Duration}ms", 
    endpoint, 
    duration
);

// Business events
_logger.LogInformation(
    "Trade signal generated. Symbol: {Symbol}, Signal: {Signal}, Confidence: {Confidence}", 
    symbol, 
    signal, 
    confidence
);

// Security events
_logger.LogWarning(
    "Rate limit exceeded for IP: {IpAddress}, Endpoint: {Endpoint}", 
    ipAddress, 
    endpoint
);

// Data quality issues
_logger.LogWarning(
    "Missing data detected. Symbol: {Symbol}, Field: {Field}", 
    symbol, 
    field
);
```

## Testing Standards

### Backend (xUnit)

```csharp
// Unit test structure
public class StockDataServiceTests
{
    private readonly Mock<IStockRepository> _mockRepository;
    private readonly Mock<ILogger<StockDataService>> _mockLogger;
    private readonly StockDataService _service;
    
    public StockDataServiceTests()
    {
        _mockRepository = new Mock<IStockRepository>();
        _mockLogger = new Mock<ILogger<StockDataService>>();
        _service = new StockDataService(_mockRepository.Object, _mockLogger.Object);
    }
    
    [Fact]
    public async Task GetStock_WithValidSymbol_ReturnsStock()
    {
        // Arrange
        var symbol = "AAPL";
        var expectedStock = new Stock { Symbol = symbol, Price = 150.00m };
        _mockRepository
            .Setup(r => r.GetBySymbol(symbol))
            .ReturnsAsync(expectedStock);
        
        // Act
        var result = await _service.GetStock(symbol);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(symbol, result.Symbol);
        Assert.Equal(150.00m, result.Price);
    }
    
    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public async Task GetStock_WithInvalidSymbol_ThrowsArgumentException(string symbol)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.GetStock(symbol));
    }
}

// Integration test
public class StockApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    
    public StockApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }
    
    [Fact]
    public async Task GetStock_ReturnsSuccessStatusCode()
    {
        // Act
        var response = await _client.GetAsync("/api/stocks/AAPL");
        
        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("AAPL", content);
    }
}
```

### Frontend (Vitest)

```typescript
// Component test
import { describe, it, expect, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import StockChart from '@/components/StockChart.vue'

describe('StockChart', () => {
  it('renders stock price correctly', () => {
    const wrapper = mount(StockChart, {
      props: {
        symbol: 'AAPL',
        price: 150.00
      }
    })
    
    expect(wrapper.text()).toContain('AAPL')
    expect(wrapper.text()).toContain('150.00')
  })
  
  it('emits update event when price changes', async () => {
    const wrapper = mount(StockChart, {
      props: {
        symbol: 'AAPL',
        price: 150.00
      }
    })
    
    await wrapper.setProps({ price: 155.00 })
    
    expect(wrapper.emitted('priceUpdate')).toBeTruthy()
    expect(wrapper.emitted('priceUpdate')?.[0]).toEqual([155.00])
  })
})

// Composable test
import { describe, it, expect } from 'vitest'
import { useStockData } from '@/composables/useStockData'

describe('useStockData', () => {
  it('fetches stock data successfully', async () => {
    const { stockData, fetchStock, loading, error } = useStockData()
    
    await fetchStock('AAPL')
    
    expect(loading.value).toBe(false)
    expect(error.value).toBeNull()
    expect(stockData.value).toBeDefined()
    expect(stockData.value?.symbol).toBe('AAPL')
  })
})
```

## Error Handling

### Backend

```csharp
// Custom exceptions
public class StockNotFoundException : Exception
{
    public string Symbol { get; }
    
    public StockNotFoundException(string symbol) 
        : base($"Stock with symbol '{symbol}' not found")
    {
        Symbol = symbol;
    }
}

// Global exception handler
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, 
        Exception exception, 
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "An unhandled exception occurred");
        
        var (statusCode, message) = exception switch
        {
            StockNotFoundException => (StatusCodes.Status404NotFound, exception.Message),
            ValidationException => (StatusCodes.Status400BadRequest, exception.Message),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            _ => (StatusCodes.Status500InternalServerError, "An error occurred")
        };
        
        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(new { error = message }, cancellationToken);
        
        return true;
    }
}

// Service-level error handling
public async Task<Result<Stock>> GetStockSafely(string symbol)
{
    try
    {
        var stock = await _repository.GetBySymbol(symbol);
        return Result<Stock>.Success(stock);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error fetching stock {Symbol}", symbol);
        return Result<Stock>.Failure($"Failed to fetch stock: {ex.Message}");
    }
}
```

### Frontend

```typescript
// Error handling composable
export function useErrorHandler() {
  const toast = useToast()
  
  function handleError(error: unknown) {
    if (error instanceof ApiError) {
      toast.error(error.message)
      console.error('API Error:', error)
    } else if (error instanceof NetworkError) {
      toast.error('Network error. Please check your connection.')
    } else {
      toast.error('An unexpected error occurred')
      console.error('Unexpected error:', error)
    }
  }
  
  return { handleError }
}

// Usage in component
const { handleError } = useErrorHandler()

async function fetchStockData() {
  try {
    const data = await api.getStock(symbol.value)
    stockData.value = data
  } catch (error) {
    handleError(error)
  }
}
```

## Performance Guidelines

### Backend

1. **Use async/await consistently**
2. **Implement caching** (Redis for distributed, IMemoryCache for in-memory)
3. **Use pagination** for large datasets
4. **Implement rate limiting**
5. **Use connection pooling** for database
6. **Profile and optimize** hot paths
7. **Use bulk operations** where possible

### Frontend

1. **Lazy load routes** and components
2. **Virtualize long lists** (vue-virtual-scroller)
3. **Debounce user inputs**
4. **Optimize chart rendering** (use canvas for large datasets)
5. **Implement infinite scrolling** for news feeds
6. **Use Web Workers** for heavy computations
7. **Cache API responses** in Pinia stores

## Security Guidelines

1. **Never commit secrets** to version control
2. **Use environment variables** for configuration
3. **Implement CORS** properly
4. **Validate all inputs** (backend and frontend)
5. **Use HTTPS** in production
6. **Implement rate limiting** on APIs
7. **Sanitize user inputs** to prevent XSS
8. **Use parameterized queries** to prevent SQL injection
9. **Implement authentication** (JWT tokens)
10. **Log security events** (failed logins, rate limit violations)

## Documentation Requirements

### Code Documentation

```csharp
/// <summary>
/// Fetches historical stock data for the specified symbol and date range.
/// </summary>
/// <param name="symbol">The stock symbol (e.g., "AAPL")</param>
/// <param name="startDate">The start date for historical data</param>
/// <param name="endDate">The end date for historical data</param>
/// <returns>A list of historical stock prices</returns>
/// <exception cref="ArgumentException">Thrown when symbol is null or empty</exception>
/// <exception cref="StockNotFoundException">Thrown when stock is not found</exception>
public async Task<List<StockPrice>> GetHistoricalData(
    string symbol, 
    DateTime startDate, 
    DateTime endDate)
{
    // Implementation
}
```

### Epic Documentation

Each epic must have:
1. **Overview** - What is being built and why
2. **User Stories** - Detailed user stories with acceptance criteria
3. **Architecture Diagram** - Mermaid diagrams showing system design
4. **API Specifications** - Endpoint definitions, request/response formats
5. **Data Models** - Entity relationships and schemas
6. **Testing Strategy** - Unit, integration, and E2E test plans
7. **Deployment Plan** - How to deploy and rollback
8. **Monitoring** - What metrics to track

## Git Workflow

### Branch Naming

- `feature/epic-name-feature-description`
- `bugfix/issue-number-bug-description`
- `hotfix/critical-issue-description`
- `refactor/component-being-refactored`

### Commit Messages

```
<type>(<scope>): <subject>

<body>

<footer>
```

Types: `feat`, `fix`, `docs`, `style`, `refactor`, `test`, `chore`

Example:
```
feat(market-data): add real-time price updates via WebSocket

- Implement WebSocket connection to Polygon.io
- Add price update handler in MarketDataService
- Update frontend to display real-time prices
- Add unit tests for WebSocket service

Closes #123
```

## Review Checklist

Before submitting PR:
- [ ] Code follows SOLID principles
- [ ] Appropriate design patterns used
- [ ] Unit tests written and passing
- [ ] Integration tests passing
- [ ] Code documented
- [ ] No hardcoded secrets
- [ ] Error handling implemented
- [ ] Logging added for important operations
- [ ] Performance considered
- [ ] Security reviewed

## Refactoring Guidelines

Refer to https://refactoring.guru/refactoring for detailed techniques.

### When to Refactor

1. **Code Smells Detected**:
   - Long methods (>20 lines)
   - Large classes (>200 lines)
   - Duplicate code
   - Long parameter lists
   - Feature envy
   - Data clumps

2. **Before Adding New Features**:
   - Clean up existing code first
   - Make the change easy, then make the easy change

3. **During Code Review**:
   - Identify improvement opportunities
   - Suggest refactoring in comments

### Refactoring Process

1. **Ensure tests exist** before refactoring
2. **Make small changes** incrementally
3. **Run tests** after each change
4. **Commit frequently** with descriptive messages
5. **Review the diff** before pushing

## Continuous Improvement

- **Weekly code reviews** to share knowledge
- **Monthly architecture reviews** to assess design decisions
- **Quarterly retrospectives** to improve processes
- **Document lessons learned** in project wiki
- **Update standards** based on team feedback
