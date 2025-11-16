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
using StockSensePro.Application.Interfaces;
using StockSensePro.Application.Services;
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

// Register configuration settings as singletons
builder.Services.AddSingleton(yahooFinanceSettings);
builder.Services.AddSingleton(cacheSettings);

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

// Register IStockDataProvider (using YahooFinanceService as the implementation)
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
