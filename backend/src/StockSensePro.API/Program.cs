using System.Text.Json;
using System.Text.Json.Serialization;
using System.Linq;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using StockSensePro.Core.Interfaces;
using StockSensePro.Application.Interfaces;
using StockSensePro.Application.Services;
using StockSensePro.Infrastructure.Data;
using StockSensePro.Infrastructure.Data.Repositories;
using StockSensePro.Infrastructure.Services;
using StockSensePro.AI.Services;
using StockSensePro.API.Filters;

var builder = WebApplication.CreateBuilder(args);

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
builder.Services.AddHttpClient<IYahooFinanceService, YahooFinanceService>();

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

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

await DatabaseInitializer.InitializeAsync(app.Services);

app.Run();
