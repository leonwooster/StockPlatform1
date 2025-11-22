using System.Text.Json;
using System.Text.Json.Serialization;
using System.Linq;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using Polly;
using Polly.Extensions.Http;
using StockSensePro.Core.Interfaces;
using StockSensePro.Core.Configuration;
using StockSensePro.Core.Enums;
using StockSensePro.Application.Interfaces;
using StockSensePro.Application.Services;
using StockSensePro.Application.Strategies;
using StockSensePro.Infrastructure.Data;
using StockSensePro.Infrastructure.Data.Repositories;
using StockSensePro.Infrastructure.Services;
using StockSensePro.AI.Services;
using StockSensePro.API.Filters;
using StockSensePro.API.Middleware;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        path: "logs/stocksensepro-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

// Bind configuration settings
var yahooFinanceSettings = builder.Configuration
    .GetSection(YahooFinanceSettings.SectionName)
    .Get<YahooFinanceSettings>() ?? new YahooFinanceSettings();

var cacheSettings = builder.Configuration
    .GetSection(CacheSettings.SectionName)
    .Get<CacheSettings>() ?? new CacheSettings();

var alphaVantageSettings = builder.Configuration
    .GetSection(AlphaVantageSettings.SectionName)
    .Get<AlphaVantageSettings>() ?? new AlphaVantageSettings();

var dataProviderSettings = builder.Configuration
    .GetSection(DataProviderSettings.SectionName)
    .Get<DataProviderSettings>() ?? new DataProviderSettings();

var providerCostSettings = builder.Configuration
    .GetSection(ProviderCostSettings.SectionName)
    .Get<ProviderCostSettings>() ?? new ProviderCostSettings();

// Register configuration settings as singletons
builder.Services.AddSingleton(yahooFinanceSettings);
builder.Services.AddSingleton(cacheSettings);
builder.Services.AddSingleton(alphaVantageSettings);
builder.Services.AddSingleton(dataProviderSettings);
builder.Services.AddSingleton(providerCostSettings);

// Register configuration settings using Options pattern for services that need IOptions<T>
builder.Services.Configure<YahooFinanceSettings>(builder.Configuration.GetSection(YahooFinanceSettings.SectionName));
builder.Services.Configure<CacheSettings>(builder.Configuration.GetSection(CacheSettings.SectionName));
builder.Services.Configure<AlphaVantageSettings>(builder.Configuration.GetSection(AlphaVantageSettings.SectionName));
builder.Services.Configure<DataProviderSettings>(builder.Configuration.GetSection(DataProviderSettings.SectionName));
builder.Services.Configure<ProviderCostSettings>(builder.Configuration.GetSection(ProviderCostSettings.SectionName));

// Register rate limit metrics as singleton for tracking across requests
builder.Services.AddSingleton<RateLimitMetrics>();

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// Add Entity Framework
builder.Services.AddDbContext<StockSenseProDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Redis
builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(
    builder.Configuration.GetConnectionString("RedisConnection") ?? "localhost"));
builder.Services.AddScoped<ICacheService, RedisCacheService>();

// Add repositories
builder.Services.AddScoped<IStockRepository, DatabaseStockRepository>();
builder.Services.AddScoped<ITradingSignalRepository, TradingSignalRepository>();
builder.Services.AddScoped<ISignalPerformanceRepository, SignalPerformanceRepository>();

// Add services
builder.Services.AddScoped<IStockService, StockService>();
builder.Services.AddScoped<IAgentService, AgentService>();
builder.Services.AddScoped<IBacktestService, BacktestService>();
builder.Services.AddScoped<IHistoricalPriceProvider, HistoricalPriceProvider>();

// Configure named HttpClients for YahooFinanceService with Polly resilience policies
var timeout = TimeSpan.FromSeconds(yahooFinanceSettings.Timeout);

builder.Services.AddHttpClient("YahooFinanceChart", client =>
{
    client.BaseAddress = new Uri($"{yahooFinanceSettings.BaseUrl}/v8/finance/chart/");
    client.Timeout = timeout;
})
.AddPolicyHandler(GetRetryPolicy(yahooFinanceSettings.MaxRetries))
.AddPolicyHandler(GetCircuitBreakerPolicy())
.AddPolicyHandler(GetTimeoutPolicy(timeout));

builder.Services.AddHttpClient("YahooFinanceQuote", client =>
{
    client.BaseAddress = new Uri($"{yahooFinanceSettings.BaseUrl}/v7/finance/quote/");
    client.Timeout = timeout;
})
.AddPolicyHandler(GetRetryPolicy(yahooFinanceSettings.MaxRetries))
.AddPolicyHandler(GetCircuitBreakerPolicy())
.AddPolicyHandler(GetTimeoutPolicy(timeout));

builder.Services.AddHttpClient("YahooFinanceSummary", client =>
{
    client.BaseAddress = new Uri($"{yahooFinanceSettings.BaseUrl}/v10/finance/quoteSummary/");
    client.Timeout = timeout;
})
.AddPolicyHandler(GetRetryPolicy(yahooFinanceSettings.MaxRetries))
.AddPolicyHandler(GetCircuitBreakerPolicy())
.AddPolicyHandler(GetTimeoutPolicy(timeout));

builder.Services.AddHttpClient("YahooFinanceSearch", client =>
{
    client.BaseAddress = new Uri("https://query2.finance.yahoo.com/v1/finance/search");
    client.Timeout = timeout;
})
.AddPolicyHandler(GetRetryPolicy(yahooFinanceSettings.MaxRetries))
.AddPolicyHandler(GetCircuitBreakerPolicy())
.AddPolicyHandler(GetTimeoutPolicy(timeout));

// Register YahooFinanceService (use mock in development if Yahoo Finance is not accessible)
var useMockYahooFinance = builder.Configuration.GetValue<bool>("YahooFinance:UseMock", false);

if (useMockYahooFinance)
{
    builder.Services.AddScoped<IYahooFinanceService, MockYahooFinanceService>();
    Log.Warning("Using MockYahooFinanceService - no real API calls will be made");
}
else
{
    builder.Services.AddScoped<IYahooFinanceService, YahooFinanceService>();
}

// Register MockYahooFinanceService explicitly for factory access
builder.Services.AddScoped<MockYahooFinanceService>();

// Configure HttpClient for AlphaVantageService with Polly resilience policies
var alphaVantageTimeout = TimeSpan.FromSeconds(alphaVantageSettings.Timeout);

builder.Services.AddHttpClient<AlphaVantageService>(client =>
{
    client.BaseAddress = new Uri(alphaVantageSettings.BaseUrl);
    client.Timeout = alphaVantageTimeout;
})
.AddPolicyHandler(GetRetryPolicy(alphaVantageSettings.MaxRetries))
.AddPolicyHandler(GetCircuitBreakerPolicy())
.AddPolicyHandler(GetTimeoutPolicy(alphaVantageTimeout));

// Register AlphaVantageService
builder.Services.AddScoped<AlphaVantageService>();

// Register Alpha Vantage rate limiter as singleton (shared across all requests)
builder.Services.AddSingleton<IAlphaVantageRateLimiter, AlphaVantageRateLimiter>();

// Register provider health monitor as singleton (shared across all requests)
builder.Services.AddSingleton<IProviderHealthMonitor, ProviderHealthMonitor>();

// Register cost tracking services as singletons (shared across all requests)
builder.Services.AddSingleton<IProviderCostCalculator, ProviderCostCalculator>();
builder.Services.AddSingleton<IProviderCostTracker, ProviderCostTracker>();

// Register provider metrics tracker as singleton (shared across all requests)
builder.Services.AddSingleton<IProviderMetricsTracker, ProviderMetricsTracker>();

// Register provider factory for multi-provider support
builder.Services.AddScoped<IStockDataProviderFactory, StockDataProviderFactory>();

// Register provider strategy implementations
builder.Services.AddScoped<PrimaryProviderStrategy>();
builder.Services.AddScoped<FallbackProviderStrategy>();
builder.Services.AddScoped<RoundRobinProviderStrategy>();
builder.Services.AddScoped<CostOptimizedProviderStrategy>();

// Register the appropriate strategy based on configuration
builder.Services.AddScoped<IDataProviderStrategy>(sp =>
{
    var settings = sp.GetRequiredService<DataProviderSettings>();
    var logger = sp.GetRequiredService<ILogger<Program>>();
    
    logger.LogInformation(
        "Configuring data provider strategy: Strategy={Strategy}, PrimaryProvider={PrimaryProvider}, FallbackProvider={FallbackProvider}",
        settings.Strategy,
        settings.PrimaryProvider,
        settings.FallbackProvider);
    
    return settings.Strategy switch
    {
        ProviderStrategyType.Primary => sp.GetRequiredService<PrimaryProviderStrategy>(),
        ProviderStrategyType.Fallback => sp.GetRequiredService<FallbackProviderStrategy>(),
        ProviderStrategyType.RoundRobin => sp.GetRequiredService<RoundRobinProviderStrategy>(),
        ProviderStrategyType.CostOptimized => sp.GetRequiredService<CostOptimizedProviderStrategy>(),
        _ => throw new InvalidOperationException($"Unknown provider strategy type: {settings.Strategy}")
    };
});

// Register IStockDataProvider (using YahooFinanceService as the implementation for backward compatibility)
// This will be replaced by strategy-based selection in StockService
builder.Services.AddScoped<IStockDataProvider>(sp => sp.GetRequiredService<IYahooFinanceService>());

// Polly policy definitions
static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(int maxRetries)
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        .WaitAndRetryAsync(
            retryCount: maxRetries,
            sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            onRetry: (outcome, timespan, retryCount, context) =>
            {
                var logger = context.GetLogger();
                if (logger != null)
                {
                    logger.LogWarning(
                        "Retry {RetryCount} after {Delay}s due to {Reason}",
                        retryCount,
                        timespan.TotalSeconds,
                        outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString() ?? "Unknown");
                }
            });
}

static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        .CircuitBreakerAsync(
            handledEventsAllowedBeforeBreaking: 5,
            durationOfBreak: TimeSpan.FromSeconds(30),
            onBreak: (outcome, duration, context) =>
            {
                var logger = context.GetLogger();
                if (logger != null)
                {
                    logger.LogWarning(
                        "Circuit breaker opened for {Duration}s due to {Reason}",
                        duration.TotalSeconds,
                        outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString() ?? "Unknown");
                }
            },
            onReset: (context) =>
            {
                var logger = context.GetLogger();
                if (logger != null)
                {
                    logger.LogInformation("Circuit breaker reset");
                }
            });
}

static IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy(TimeSpan timeout)
{
    return Policy.TimeoutAsync<HttpResponseMessage>(timeout);
}

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "StockSensePro API",
        Version = "v1",
        Description = "API documentation for StockSense Pro, providing endpoints for stock data and AI trading insights.",
        Contact = new OpenApiContact
        {
            Name = "StockSense Pro Team",
            Email = "support@stocksensepro.dev"
        }
    });

    c.SchemaFilter<StockSchemaExampleFilter>();
    c.SchemaFilter<AgentAnalysisResultSchemaFilter>();
    c.SchemaFilter<AnalyzeRequestSchemaFilter>();

    c.MapType<SignalType>(() => new OpenApiSchema
    {
        Type = "string",
        Enum = Enum.GetNames(typeof(SignalType))
            .Select(name => (IOpenApiAny)new OpenApiString(name))
            .ToList()
    });

    c.MapType<AgentType>(() => new OpenApiSchema
    {
        Type = "string",
        Enum = Enum.GetNames(typeof(AgentType))
            .Select(name => (IOpenApiAny)new OpenApiString(name))
            .ToList()
    });

    c.MapType<RiskLevel>(() => new OpenApiSchema
    {
        Type = "string",
        Enum = Enum.GetNames(typeof(RiskLevel))
            .Select(name => (IOpenApiAny)new OpenApiString(name))
            .ToList()
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// CORS must come before other middleware
app.UseCors("AllowAll");

// HTTPS redirection (disabled in development to avoid issues)
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Add rate limiting middleware
app.UseMiddleware<RateLimitMiddleware>();

app.UseAuthorization();
app.MapControllers();

try
{
    Log.Information("Starting StockSensePro API");
    
    // Validate API key configuration on startup
    ValidateApiKeyConfiguration(alphaVantageSettings);
    
    // Validate provider configuration on startup
    using (var scope = app.Services.CreateScope())
    {
        var settings = scope.ServiceProvider.GetRequiredService<DataProviderSettings>();
        var factory = scope.ServiceProvider.GetRequiredService<IStockDataProviderFactory>();
        var healthMonitor = scope.ServiceProvider.GetRequiredService<IProviderHealthMonitor>();
        
        // Validate that primary provider is available
        var availableProviders = factory.GetAvailableProviders().ToList();
        if (!availableProviders.Contains(settings.PrimaryProvider))
        {
            Log.Warning(
                "Configured primary provider {PrimaryProvider} is not available. Available providers: {AvailableProviders}",
                settings.PrimaryProvider,
                string.Join(", ", availableProviders));
        }
        
        // Validate fallback provider if configured
        if (settings.FallbackProvider.HasValue && !availableProviders.Contains(settings.FallbackProvider.Value))
        {
            Log.Warning(
                "Configured fallback provider {FallbackProvider} is not available. Available providers: {AvailableProviders}",
                settings.FallbackProvider.Value,
                string.Join(", ", availableProviders));
        }
        
        // Start background health monitoring
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
        
        Log.Information(
            "Multi-provider configuration validated: Strategy={Strategy}, PrimaryProvider={PrimaryProvider}, FallbackProvider={FallbackProvider}, AvailableProviders={AvailableProviders}",
            settings.Strategy,
            settings.PrimaryProvider,
            settings.FallbackProvider,
            string.Join(", ", availableProviders));
    }
    
    await DatabaseInitializer.InitializeAsync(app.Services);
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

/// <summary>
/// Validates API key configuration on startup to catch configuration issues early
/// </summary>
static void ValidateApiKeyConfiguration(AlphaVantageSettings alphaVantageSettings)
{
    Log.Information("Validating API key configuration...");
    
    // Validate Alpha Vantage API key
    if (alphaVantageSettings.Enabled)
    {
        if (string.IsNullOrWhiteSpace(alphaVantageSettings.ApiKey))
        {
            Log.Warning(
                "Alpha Vantage is enabled but API key is not configured. " +
                "Provider will not be available. " +
                "Set the API key using User Secrets (development) or Environment Variables (production). " +
                "See docs/API_KEY_SECURITY.md for details.");
        }
        else if (alphaVantageSettings.ApiKey.Length < 8)
        {
            Log.Warning(
                "Alpha Vantage API key appears to be invalid (too short). " +
                "Expected at least 8 characters, got {Length}. " +
                "Verify your API key at https://www.alphavantage.co/support/#api-key",
                alphaVantageSettings.ApiKey.Length);
        }
        else if (alphaVantageSettings.ApiKey.Contains("YOUR_") || 
                 alphaVantageSettings.ApiKey.Contains("REPLACE") ||
                 alphaVantageSettings.ApiKey == "demo")
        {
            Log.Warning(
                "Alpha Vantage API key appears to be a placeholder value. " +
                "Replace with your actual API key from https://www.alphavantage.co/support/#api-key");
        }
        else
        {
            // Mask the API key for logging (show only first 4 and last 4 characters)
            var maskedKey = alphaVantageSettings.ApiKey.Length > 8
                ? $"{alphaVantageSettings.ApiKey[..4]}...{alphaVantageSettings.ApiKey[^4..]}"
                : "****";
            
            Log.Information(
                "Alpha Vantage API key configured and validated: Key={MaskedKey}, Length={Length}",
                maskedKey,
                alphaVantageSettings.ApiKey.Length);
        }
    }
    else
    {
        Log.Information("Alpha Vantage provider is disabled in configuration");
    }
    
    Log.Information("API key validation complete");
}

// Extension method for Polly context to get logger
public static class PollyContextExtensions
{
    private const string LoggerKey = "ILogger";

    public static Context WithLogger(this Context context, Microsoft.Extensions.Logging.ILogger logger)
    {
        context[LoggerKey] = logger;
        return context;
    }

    public static Microsoft.Extensions.Logging.ILogger? GetLogger(this Context context)
    {
        if (context.TryGetValue(LoggerKey, out var logger))
        {
            return logger as Microsoft.Extensions.Logging.ILogger;
        }
        return null;
    }
}

// Make Program class accessible to integration tests
public partial class Program { }
