# Yahoo Finance API Troubleshooting

## Issue: API Timeouts

Based on the logs, Yahoo Finance API requests are timing out after 10 seconds:

```
TaskCanceledException: The request was canceled due to the configured HttpClient.Timeout of 10 seconds elapsing.
```

## Possible Causes

### 1. Network/Firewall Blocking

Yahoo Finance might be blocked by:
- Corporate firewall
- Antivirus software
- Windows Firewall
- ISP blocking

**Test:**
```bash
# Test if you can reach Yahoo Finance
curl -v https://query1.finance.yahoo.com/v8/finance/chart/AAPL

# Or in PowerShell
Invoke-WebRequest -Uri "https://query1.finance.yahoo.com/v8/finance/chart/AAPL" -UseBasicParsing
```

### 2. DNS Resolution Issues

The domain might not be resolving properly.

**Test:**
```bash
# Test DNS resolution
nslookup query1.finance.yahoo.com

# Or ping
ping query1.finance.yahoo.com
```

### 3. Yahoo Finance Rate Limiting

Yahoo Finance might be rate limiting or blocking your IP address.

**Symptoms:**
- Requests timeout consistently
- Circuit breaker opens after 5 failures
- All subsequent requests fail immediately

### 4. Proxy/VPN Issues

If you're behind a proxy or VPN, it might be interfering.

**Test:**
- Try disabling VPN temporarily
- Check proxy settings

## Solutions

### Solution 1: Increase Timeout

If Yahoo Finance is just slow, increase the timeout:

**appsettings.json:**
```json
{
  "YahooFinance": {
    "Timeout": 30
  }
}
```

### Solution 2: Use Alternative Data Source

If Yahoo Finance is consistently blocked, consider using an alternative:

1. **Alpha Vantage** (free tier available)
2. **IEX Cloud** (free tier available)
3. **Finnhub** (free tier available)
4. **Twelve Data** (free tier available)

### Solution 3: Configure Proxy

If you need to use a proxy:

**Program.cs:**
```csharp
builder.Services.AddHttpClient("YahooFinanceChart", client =>
{
    client.BaseAddress = new Uri($"{yahooFinanceSettings.BaseUrl}/v8/finance/chart/");
    client.Timeout = timeout;
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    Proxy = new WebProxy("http://your-proxy:8080"),
    UseProxy = true
});
```

### Solution 4: Add User-Agent Header

Yahoo Finance might require a User-Agent header:

**YahooFinanceService.cs:**
```csharp
var request = new HttpRequestMessage(HttpMethod.Get, symbol);
request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
```

### Solution 5: Use Mock Data for Development

For development/testing, use mock data:

**Create a MockYahooFinanceService:**
```csharp
public class MockYahooFinanceService : IYahooFinanceService
{
    public async Task<MarketData> GetQuoteAsync(string symbol, CancellationToken cancellationToken = default)
    {
        await Task.Delay(100); // Simulate network delay
        
        return new MarketData
        {
            Symbol = symbol,
            CurrentPrice = 150.00m + new Random().Next(-10, 10),
            Change = 2.50m,
            ChangePercent = 1.69m,
            Volume = 50000000,
            // ... other properties
        };
    }
    // ... implement other methods
}
```

**Register in Program.cs:**
```csharp
if (app.Environment.IsDevelopment())
{
    builder.Services.AddScoped<IYahooFinanceService, MockYahooFinanceService>();
}
else
{
    builder.Services.AddScoped<IYahooFinanceService, YahooFinanceService>();
}
```

## Diagnostic Steps

### Step 1: Test Network Connectivity

```powershell
# Test basic connectivity
Test-NetConnection query1.finance.yahoo.com -Port 443

# Test HTTP request
$response = Invoke-WebRequest -Uri "https://query1.finance.yahoo.com/v8/finance/chart/AAPL" -UseBasicParsing -TimeoutSec 30
$response.StatusCode
```

### Step 2: Check Firewall Rules

```powershell
# Check Windows Firewall
Get-NetFirewallRule | Where-Object {$_.DisplayName -like "*dotnet*"}

# Temporarily disable firewall (for testing only!)
Set-NetFirewallProfile -Profile Domain,Public,Private -Enabled False
# Remember to re-enable: Set-NetFirewallProfile -Profile Domain,Public,Private -Enabled True
```

### Step 3: Test with curl

```bash
# Verbose output to see what's happening
curl -v -H "User-Agent: Mozilla/5.0" https://query1.finance.yahoo.com/v8/finance/chart/AAPL

# With timeout
curl --max-time 30 https://query1.finance.yahoo.com/v8/finance/chart/AAPL
```

### Step 4: Check DNS

```powershell
# Resolve DNS
Resolve-DnsName query1.finance.yahoo.com

# Try different DNS server
Resolve-DnsName query1.finance.yahoo.com -Server 8.8.8.8
```

### Step 5: Test from Different Network

- Try from mobile hotspot
- Try from different WiFi network
- Try from VPN

## Quick Fix: Use Mock Data

I'll create a mock service you can use for development:

1. The mock service will return realistic fake data
2. No network calls needed
3. Instant responses
4. Perfect for development and testing

Would you like me to implement this?

## Recommended Actions

1. **Immediate**: Test network connectivity using the commands above
2. **Short-term**: Increase timeout to 30 seconds
3. **Long-term**: Consider alternative data sources or implement caching with longer TTL

## Common Error Patterns

### Circuit Breaker Open
```
BrokenCircuitException: The circuit is now open and is not allowing calls.
```
**Cause**: 5 consecutive failures triggered circuit breaker
**Solution**: Wait 30 seconds for circuit to reset, or restart the application

### All Requests Failing
**Cause**: Circuit breaker is open
**Solution**: Fix underlying connectivity issue, then restart

### Intermittent Failures
**Cause**: Network instability or rate limiting
**Solution**: Implement exponential backoff, increase retry count

## Monitoring

Check the logs for patterns:
```bash
# Count timeout errors
findstr /c:"TaskCanceledException" logs\stocksensepro-*.log | find /c /v ""

# Check circuit breaker events
findstr /c:"Circuit breaker" logs\stocksensepro-*.log

# View recent errors
findstr /c:"[ERR]" logs\stocksensepro-20251117.log | more
```

## Support

If none of these solutions work:
1. Check if Yahoo Finance is down: https://downdetector.com/status/yahoo/
2. Try alternative data sources
3. Use mock data for development
4. Contact your network administrator about firewall rules
