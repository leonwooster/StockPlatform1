# Alpha Vantage Quick Start Guide

## Get Your API Key

1. Visit [Alpha Vantage](https://www.alphavantage.co/support/#api-key)
2. Enter your email and get your free API key
3. Free tier includes: **25 requests/day, 5 requests/minute**

## Setup (Choose One Method)

### Method 1: Automated Setup (Recommended)

Run the setup script from the `backend` directory:

```powershell
cd backend
.\setup-alphavantage.ps1
```

The script will:
- Prompt for your API key
- Configure User Secrets securely
- Let you choose your provider strategy
- Show your configuration

### Method 2: Manual Setup

Navigate to the API project:

```bash
cd backend/src/StockSensePro.API
```

Set your API key using User Secrets:

```bash
# Set API key (REQUIRED)
dotnet user-secrets set "AlphaVantage:ApiKey" "YOUR_API_KEY_HERE"

# Enable Alpha Vantage (REQUIRED)
dotnet user-secrets set "AlphaVantage:Enabled" "true"
```

**Choose a provider strategy:**

**Option A: Yahoo Finance Primary (Recommended for Free Tier)**
```bash
dotnet user-secrets set "DataProvider:PrimaryProvider" "YahooFinance"
dotnet user-secrets set "DataProvider:FallbackProvider" "AlphaVantage"
dotnet user-secrets set "DataProvider:Strategy" "Fallback"
```

**Option B: Alpha Vantage Primary**
```bash
dotnet user-secrets set "DataProvider:PrimaryProvider" "AlphaVantage"
dotnet user-secrets set "DataProvider:FallbackProvider" "YahooFinance"
dotnet user-secrets set "DataProvider:Strategy" "Fallback"
```

**Option C: Alpha Vantage Only**
```bash
dotnet user-secrets set "DataProvider:PrimaryProvider" "AlphaVantage"
dotnet user-secrets set "DataProvider:Strategy" "Primary"
```

## Verify Configuration

Check your User Secrets:

```bash
cd backend/src/StockSensePro.API
dotnet user-secrets list
```

You should see:
```
AlphaVantage:ApiKey = av-****1234
AlphaVantage:Enabled = true
DataProvider:PrimaryProvider = AlphaVantage
DataProvider:Strategy = Fallback
```

## Run the Application

```bash
cd backend/src/StockSensePro.API
dotnet run
```

## Test It Works

### 1. Check Health
```bash
curl http://localhost:5566/api/health
```

### 2. Check Metrics
```bash
curl http://localhost:5566/api/health/metrics
```

Look for:
```json
{
  "currentProvider": "alphaVantage",
  "providers": {
    "alphaVantage": {
      "isHealthy": true,
      "rateLimitRemaining": {
        "daily": 25,
        "minute": 5
      }
    }
  }
}
```

### 3. Get a Stock Quote
```bash
curl http://localhost:5566/api/stocks/quote/AAPL
```

## Configuration Options

### Rate Limits (Free Tier)

The default configuration in `appsettings.json` is set for the free tier:

```json
{
  "AlphaVantage": {
    "RateLimit": {
      "RequestsPerMinute": 5,
      "RequestsPerDay": 25
    }
  }
}
```

**If you upgrade to a paid tier**, update these values in User Secrets:

```bash
# Basic tier ($49.99/month)
dotnet user-secrets set "AlphaVantage:RateLimit:RequestsPerMinute" "75"
dotnet user-secrets set "AlphaVantage:RateLimit:RequestsPerDay" "500"

# Pro tier ($149.99/month)
dotnet user-secrets set "AlphaVantage:RateLimit:RequestsPerMinute" "150"
dotnet user-secrets set "AlphaVantage:RateLimit:RequestsPerDay" "1200"
```

### Data Enrichment

Enable additional features:

```bash
# Get bid/ask prices from Yahoo Finance when Alpha Vantage doesn't provide them
dotnet user-secrets set "AlphaVantage:DataEnrichment:EnableBidAskEnrichment" "true"

# Calculate 52-week high/low from historical data
dotnet user-secrets set "AlphaVantage:DataEnrichment:EnableCalculated52WeekRange" "true"

# Calculate average volume
dotnet user-secrets set "AlphaVantage:DataEnrichment:EnableCalculatedAverageVolume" "true"
```

## Troubleshooting

### "Invalid API key" Error

```bash
# Verify your API key is set correctly
dotnet user-secrets list | grep AlphaVantage:ApiKey

# Re-set if needed
dotnet user-secrets set "AlphaVantage:ApiKey" "YOUR_CORRECT_KEY"
```

### "Rate limit exceeded" Warning

This is normal for the free tier. The system will:
1. Return cached data if available
2. Fall back to Yahoo Finance (if configured)
3. Queue the request for later

To reduce rate limit hits:
- Use Yahoo Finance as primary
- Increase cache TTLs
- Enable data enrichment

### Provider Not Available

```bash
# Make sure Alpha Vantage is enabled
dotnet user-secrets set "AlphaVantage:Enabled" "true"

# Restart the application
```

## Production Deployment

**DO NOT use User Secrets in production!**

For production, use:
- **Azure Key Vault** (recommended)
- **Environment Variables**
- **AWS Secrets Manager**
- **HashiCorp Vault**

See [Deployment Guide](./DEPLOYMENT_GUIDE.md) for details.

## Cost Optimization Tips

### Free Tier (25 requests/day)

1. **Use Yahoo Finance as primary**
   ```bash
   dotnet user-secrets set "DataProvider:PrimaryProvider" "YahooFinance"
   dotnet user-secrets set "DataProvider:FallbackProvider" "AlphaVantage"
   ```

2. **Increase cache TTLs**
   ```bash
   dotnet user-secrets set "Cache:AlphaVantage:QuoteTTL" "1800"  # 30 minutes
   ```

3. **Enable data enrichment** (reduces duplicate calls)
   ```bash
   dotnet user-secrets set "AlphaVantage:DataEnrichment:EnableCalculated52WeekRange" "true"
   ```

### Paid Tier

1. **Use Alpha Vantage as primary** for better data quality
2. **Monitor costs** via metrics endpoint
3. **Set cost alerts** in configuration

## Next Steps

- Read the [User Guide](./ALPHA_VANTAGE_USER_GUIDE.md) for detailed configuration
- Read the [Developer Guide](./ALPHA_VANTAGE_DEVELOPER_GUIDE.md) for architecture details
- Check [Deployment Guide](./DEPLOYMENT_GUIDE.md) for production setup

## Support

- Alpha Vantage Support: https://www.alphavantage.co/support/
- API Documentation: https://www.alphavantage.co/documentation/

---

**Last Updated**: November 22, 2025  
**Version**: 1.0

