# Quick Fix: Yahoo Finance Not Accessible

## Problem

Yahoo Finance API is timing out, causing all stock data requests to fail.

## Immediate Solution: Use Mock Data

I've implemented a mock Yahoo Finance service that returns realistic fake data without making actual API calls.

### How to Enable

The mock service is **already enabled by default in Development mode**.

**Configuration** (`appsettings.Development.json`):
```json
{
  "YahooFinance": {
    "UseMock": true
  }
}
```

### What You Get

The mock service provides:
- ✅ Realistic stock prices with random variations
- ✅ Historical price data
- ✅ Company fundamentals
- ✅ Company profiles
- ✅ Symbol search
- ✅ Instant responses (no network delays)
- ✅ Always healthy status

### Restart the Backend

```bash
cd backend/src/StockSensePro.API
dotnet run
```

You should see this log message:
```
[WRN] Using MockYahooFinanceService - no real API calls will be made
```

### Verify It's Working

1. **Check System Status**: Navigate to `/system-status`
   - All services should show as "Healthy" (green)
   
2. **Check Dashboard**: Navigate to `/`
   - Market data should load instantly
   - Prices will change slightly on each refresh

3. **Check Logs**: Look for "Mock:" prefix in logs
   ```
   [INF] Mock: Fetching quote for AAPL
   ```

## Switching Back to Real Yahoo Finance

When you want to use real Yahoo Finance data:

**Option 1: Change Configuration**

Edit `appsettings.Development.json`:
```json
{
  "YahooFinance": {
    "UseMock": false,
    "Timeout": 30
  }
}
```

**Option 2: Use Production Mode**

```bash
dotnet run --environment Production
```

## Troubleshooting Real Yahoo Finance

If you want to fix the Yahoo Finance connectivity issue, see:
- `YAHOO_FINANCE_TROUBLESHOOTING.md` - Detailed troubleshooting guide

### Quick Tests

**Test 1: Can you reach Yahoo Finance?**
```powershell
Test-NetConnection query1.finance.yahoo.com -Port 443
```

**Test 2: Can you make HTTP requests?**
```powershell
Invoke-WebRequest -Uri "https://query1.finance.yahoo.com/v8/finance/chart/AAPL" -UseBasicParsing -TimeoutSec 30
```

**Test 3: DNS Resolution**
```powershell
Resolve-DnsName query1.finance.yahoo.com
```

If any of these fail, you have a network/firewall issue.

## Benefits of Mock Service

### For Development
- ✅ No network dependency
- ✅ Instant responses
- ✅ Consistent data for testing
- ✅ No rate limiting
- ✅ Works offline

### For Testing
- ✅ Predictable data
- ✅ No external API costs
- ✅ Fast test execution
- ✅ No flaky tests due to network issues

### For Demos
- ✅ Always works
- ✅ No API key needed
- ✅ No rate limits
- ✅ Reliable performance

## Mock Data Details

### Supported Symbols
- AAPL, MSFT, GOOGL, AMZN, NVDA, TSLA, META, AMD, SMCI
- ^GSPC (S&P 500), ^NDX (NASDAQ 100), ^DJI (Dow Jones)

### Data Characteristics
- Prices vary randomly by ±5% on each request
- Historical data includes realistic OHLCV patterns
- Fundamentals include P/E ratios, market cap, etc.
- Company profiles include sector, industry, description

### Limitations
- Data is not real-time
- Prices are randomly generated
- No actual market correlation
- Limited symbol support

## Production Deployment

**Important:** Always use real Yahoo Finance in production!

`appsettings.Production.json`:
```json
{
  "YahooFinance": {
    "UseMock": false,
    "Timeout": 30,
    "MaxRetries": 3
  }
}
```

## Summary

1. **Mock service is enabled by default in Development**
2. **Restart the backend to apply changes**
3. **Check System Status to verify all services are healthy**
4. **Use mock data for development, real data for production**

Your application should now work perfectly with mock data!
