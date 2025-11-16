# Logging Configuration

## Overview

StockSensePro uses **Serilog** for structured logging with rolling file support. All application logs are written to both the console and rolling log files.

## Log File Location

Logs are stored in the `logs/` directory relative to the API project:

```
backend/src/StockSensePro.API/logs/
├── stocksensepro-20251116.log
├── stocksensepro-20251117.log
└── stocksensepro-20251118.log
```

## Rolling File Configuration

- **Rolling Interval**: Daily (new file each day)
- **File Name Pattern**: `stocksensepro-YYYYMMDD.log`
- **Retention**: 30 days (older files are automatically deleted)
- **Format**: Structured text with timestamp, log level, message, and exception details

## Log Format

Each log entry follows this format:

```
2025-11-16 16:30:45.123 +00:00 [INF] Application started successfully
2025-11-16 16:30:46.456 +00:00 [WRN] Rate limit exceeded for /api/stocks/AAPL/quote
2025-11-16 16:30:47.789 +00:00 [ERR] Failed to fetch quote for AAPL
System.Net.Http.HttpRequestException: Request timeout
   at StockSensePro.Infrastructure.Services.YahooFinanceService.GetQuoteAsync(...)
```

### Log Levels

- **VRB** (Verbose): Detailed diagnostic information
- **DBG** (Debug): Debug-level information
- **INF** (Information): General informational messages
- **WRN** (Warning): Warning messages for potentially harmful situations
- **ERR** (Error): Error messages for failures
- **FTL** (Fatal): Critical errors that cause application termination

## Configuration

### appsettings.json

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.AspNetCore": "Warning",
        "System": "Warning"
      }
    }
  }
}
```

### Environment-Specific Configuration

**Development** (`appsettings.Development.json`):
```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Debug"
    }
  }
}
```

**Production** (`appsettings.Production.json`):
```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    }
  }
}
```

## What Gets Logged

### Application Startup/Shutdown
- Application start
- Configuration loading
- Database initialization
- Application termination (normal and unexpected)

### API Requests
- HTTP requests (method, path, status code, duration)
- Request validation errors
- Authentication/authorization failures

### Yahoo Finance Integration
- API calls to Yahoo Finance
- Response times
- Rate limiting events
- Circuit breaker state changes
- Retry attempts
- Timeout errors

### Database Operations
- Connection issues
- Query execution errors
- Transaction failures

### Cache Operations
- Redis connection status
- Cache hits/misses
- Cache errors

### Rate Limiting
- Rate limit exceeded events
- Throttling actions
- Request queuing

### Errors and Exceptions
- All unhandled exceptions
- Service-specific errors
- Integration failures

## Viewing Logs

### Real-time (Console)

When running the application, logs are output to the console in real-time:

```bash
cd backend/src/StockSensePro.API
dotnet run
```

### Log Files

View the current day's log:

```bash
# Windows
type backend\src\StockSensePro.API\logs\stocksensepro-20251116.log

# Linux/Mac
cat backend/src/StockSensePro.API/logs/stocksensepro-20251116.log
```

Tail the log file (follow new entries):

```bash
# Windows PowerShell
Get-Content backend\src\StockSensePro.API\logs\stocksensepro-20251116.log -Wait -Tail 50

# Linux/Mac
tail -f backend/src/StockSensePro.API/logs/stocksensepro-20251116.log
```

### Search Logs

Search for specific errors:

```bash
# Windows
findstr /i "error" backend\src\StockSensePro.API\logs\stocksensepro-*.log

# Linux/Mac
grep -i "error" backend/src/StockSensePro.API/logs/stocksensepro-*.log
```

Search for specific symbols:

```bash
# Windows
findstr "AAPL" backend\src\StockSensePro.API\logs\stocksensepro-*.log

# Linux/Mac
grep "AAPL" backend/src/StockSensePro.API/logs/stocksensepro-*.log
```

## Log Analysis

### Common Patterns

**Rate Limiting:**
```
[WRN] Rate limit exceeded for {Path}. Window: {Window}, Retry after: {RetryAfter}s
```

**Yahoo Finance Errors:**
```
[ERR] Error fetching quote for symbol: {Symbol}
```

**Circuit Breaker:**
```
[WRN] Circuit breaker opened for {Duration}s due to {Reason}
[INF] Circuit breaker reset
```

**Database Issues:**
```
[ERR] Database connection failed
[ERR] Query execution failed for {Query}
```

## Troubleshooting

### Logs Not Being Created

1. Check write permissions on the `logs/` directory
2. Verify Serilog configuration in `appsettings.json`
3. Check for startup errors in the console

### Log Files Growing Too Large

The default configuration retains 30 days of logs. To change this:

```csharp
.WriteTo.File(
    path: "logs/stocksensepro-.log",
    rollingInterval: RollingInterval.Day,
    retainedFileCountLimit: 7,  // Keep only 7 days
    fileSizeLimitBytes: 10_000_000,  // 10MB per file
    rollOnFileSizeLimit: true
)
```

### Performance Impact

Serilog is designed for high performance with minimal impact:
- Asynchronous writing
- Buffered I/O
- Structured logging (no string concatenation)

If you experience performance issues, consider:
- Reducing log level in production
- Using async sinks
- Implementing log sampling for high-volume endpoints

## Advanced Configuration

### Adding Additional Sinks

**Email Alerts for Errors:**
```bash
dotnet add package Serilog.Sinks.Email
```

```csharp
.WriteTo.Email(
    fromEmail: "alerts@stocksensepro.com",
    toEmail: "admin@stocksensepro.com",
    mailServer: "smtp.gmail.com",
    restrictedToMinimumLevel: LogEventLevel.Error
)
```

**Elasticsearch Integration:**
```bash
dotnet add package Serilog.Sinks.Elasticsearch
```

```csharp
.WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri("http://localhost:9200"))
{
    AutoRegisterTemplate = true,
    IndexFormat = "stocksensepro-{0:yyyy.MM.dd}"
})
```

### Structured Logging Examples

```csharp
// Good - Structured
_logger.LogInformation("Fetching quote for {Symbol} at {Timestamp}", symbol, DateTime.UtcNow);

// Bad - String interpolation
_logger.LogInformation($"Fetching quote for {symbol} at {DateTime.UtcNow}");
```

## Monitoring and Alerts

### Log Monitoring Tools

- **Seq**: Free for development, structured log viewer
- **Elasticsearch + Kibana**: Full-featured log analysis
- **Azure Application Insights**: Cloud-based monitoring
- **Datadog**: Enterprise monitoring solution

### Setting Up Seq (Recommended for Development)

1. Install Seq:
   ```bash
   docker run --name seq -d --restart unless-stopped -e ACCEPT_EULA=Y -p 5341:80 datalust/seq:latest
   ```

2. Add Seq sink:
   ```bash
   dotnet add package Serilog.Sinks.Seq
   ```

3. Configure:
   ```csharp
   .WriteTo.Seq("http://localhost:5341")
   ```

4. View logs at: `http://localhost:5341`

## Best Practices

1. **Use Structured Logging**: Always use message templates with parameters
2. **Appropriate Log Levels**: Don't log everything as Information
3. **Include Context**: Add relevant properties (symbol, userId, requestId)
4. **Avoid Sensitive Data**: Never log passwords, API keys, or PII
5. **Performance**: Use async logging for high-throughput scenarios
6. **Retention**: Balance storage costs with compliance requirements
7. **Monitoring**: Set up alerts for critical errors

## Security Considerations

- Log files may contain sensitive information
- Restrict access to the `logs/` directory
- Consider encrypting logs at rest
- Implement log rotation and secure deletion
- Comply with data retention policies (GDPR, etc.)

## Support

For issues with logging:
1. Check console output for Serilog initialization errors
2. Verify file permissions
3. Review `appsettings.json` configuration
4. Check Serilog documentation: https://serilog.net/
