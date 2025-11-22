# Alpha Vantage Integration - User Guide

## Table of Contents

1. [Overview](#overview)
2. [Getting Started](#getting-started)
3. [Provider Configuration](#provider-configuration)
4. [Strategy Selection](#strategy-selection)
5. [API Key Setup](#api-key-setup)
6. [Rate Limit Management](#rate-limit-management)
7. [Cost Optimization Tips](#cost-optimization-tips)
8. [Monitoring and Troubleshooting](#monitoring-and-troubleshooting)
9. [FAQ](#faq)

---

## Overview

StockSensePro now supports multiple stock data providers, including Alpha Vantage as an alternative to Yahoo Finance. This guide will help you configure and optimize your data provider setup.

### Why Use Alpha Vantage?

- **Reliability**: Professional-grade API with guaranteed uptime
- **Data Quality**: High-quality, verified market data
- **Flexibility**: Multiple data endpoints and customization options
- **Fallback Support**: Automatic failover when Yahoo Finance is unavailable

### Key Features

- ✅ Multiple provider support (Yahoo Finance, Alpha Vantage, Mock)
- ✅ Automatic provider switching and fallback
- ✅ Intelligent caching to minimize API usage
- ✅ Rate limit management and enforcement
- ✅ Cost tracking and optimization
- ✅ Health monitoring and metrics

---

## Getting Started

### Prerequisites

1. .NET 8.0 SDK installed
2. Redis server running (for caching)
3. Alpha Vantage API key (free or premium)

### Quick Start

1. **Get an API Key**: Visit [Alpha Vantage](https://www.alphavantage.co/support/#api-key) and sign up for a free API key

2. **Configure the Provider**: Add your API key to user secrets (development) or environment variables (production)

3. **Update Configuration**: Set Alpha Vantage as your primary provider in `appsettings.json`

4. **Start the Application**: The system will automatically use Alpha Vantage for stock data

---

## Provider Configuration

### Configuration File Structure

Edit your `appsettings.json` to configure data providers:

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
      "EnableCalculatedFields": true,
      "CalculatedFieldsCacheTTL": 86400
    }
  }
}
```

### Provider Types

| Provider | Description | Cost | Rate Limits |
|----------|-------------|------|-------------|
| **YahooFinance** | Free, unlimited stock data | Free | None (subject to fair use) |
| **AlphaVantage** | Professional API with guaranteed uptime | Free tier: $0<br>Premium: $49.99+/month | Free: 25/day, 5/min<br>Premium: 500+/day |
| **Mock** | Test data for development | Free | None |

### Configuration Options

#### DataProvider Section

- **PrimaryProvider**: The main data provider to use (`YahooFinance`, `AlphaVantage`, or `Mock`)
- **FallbackProvider**: Secondary provider used when primary fails (optional)
- **Strategy**: Provider selection strategy (see [Strategy Selection](#strategy-selection))
- **EnableAutomaticFallback**: Automatically switch to fallback provider on errors
- **HealthCheckIntervalSeconds**: How often to check provider health (default: 60)

#### AlphaVantage Section

- **ApiKey**: Your Alpha Vantage API key (store securely, see [API Key Setup](#api-key-setup))
- **BaseUrl**: API endpoint (default: `https://www.alphavantage.co/query`)
- **Timeout**: Request timeout in seconds (default: 10)
- **MaxRetries**: Number of retry attempts on transient errors (default: 3)
- **Enabled**: Enable/disable Alpha Vantage provider
- **RateLimit**: Configure rate limits based on your tier
- **DataEnrichment**: Configure data enrichment features

### Environment-Specific Configuration

#### Development (appsettings.Development.json)

```json
{
  "DataProvider": {
    "PrimaryProvider": "Mock",
    "Strategy": "Primary"
  }
}
```

#### Production (appsettings.Production.json)

```json
{
  "DataProvider": {
    "PrimaryProvider": "AlphaVantage",
    "FallbackProvider": "YahooFinance",
    "Strategy": "Fallback",
    "EnableAutomaticFallback": true
  }
}
```

---

## Strategy Selection

Provider strategies determine how the system selects which data provider to use for each request.

### Available Strategies

#### 1. Primary Strategy

**Use Case**: Single provider, no fallback needed

```json
{
  "DataProvider": {
    "PrimaryProvider": "AlphaVantage",
    "Strategy": "Primary"
  }
}
```

**Behavior**:
- Always uses the configured primary provider
- Fails if primary provider is unavailable
- Simplest configuration

**Best For**:
- Development environments
- When you have a reliable premium provider
- Testing specific provider behavior

#### 2. Fallback Strategy (Recommended)

**Use Case**: High availability with automatic failover

```json
{
  "DataProvider": {
    "PrimaryProvider": "AlphaVantage",
    "FallbackProvider": "YahooFinance",
    "Strategy": "Fallback",
    "EnableAutomaticFallback": true
  }
}
```

**Behavior**:
- Uses primary provider when healthy
- Automatically switches to fallback on errors
- Monitors health and recovers automatically
- Returns to primary when it becomes healthy

**Best For**:
- Production environments
- Maximum uptime requirements
- Cost-conscious deployments (use free provider as fallback)

#### 3. Round Robin Strategy

**Use Case**: Load distribution across multiple providers

```json
{
  "DataProvider": {
    "PrimaryProvider": "AlphaVantage",
    "FallbackProvider": "YahooFinance",
    "Strategy": "RoundRobin"
  }
}
```

**Behavior**:
- Distributes requests evenly across all healthy providers
- Skips unhealthy providers automatically
- Balances API usage across providers

**Best For**:
- High-volume applications
- Multiple premium API keys
- Load testing and comparison

#### 4. Cost Optimized Strategy

**Use Case**: Minimize API costs while maintaining reliability

```json
{
  "DataProvider": {
    "PrimaryProvider": "YahooFinance",
    "FallbackProvider": "AlphaVantage",
    "Strategy": "CostOptimized"
  }
}
```

**Behavior**:
- Prefers free providers when available
- Uses paid providers only when necessary
- Respects rate limits to avoid overages
- Tracks costs and optimizes selection

**Best For**:
- Budget-conscious deployments
- Applications with variable load
- Development and staging environments

### Strategy Comparison

| Strategy | Uptime | Cost | Complexity | Use Case |
|----------|--------|------|------------|----------|
| Primary | Medium | Low | Low | Development, single provider |
| Fallback | High | Medium | Medium | Production, high availability |
| RoundRobin | High | High | Medium | High volume, load distribution |
| CostOptimized | High | Low | High | Budget-conscious, variable load |

---

## API Key Setup

### Development Environment

Use .NET User Secrets to store your API key securely:

```bash
# Navigate to your project directory
cd backend/src/StockSensePro.Api

# Set the API key
dotnet user-secrets set "AlphaVantage:ApiKey" "YOUR_API_KEY_HERE"

# Verify it was set
dotnet user-secrets list
```

**Advantages**:
- Keys are stored outside source control
- Easy to manage per-developer
- Automatic integration with configuration system

### Production Environment

#### Option 1: Environment Variables (Recommended)

Set environment variables on your server:

```bash
# Linux/Mac
export AlphaVantage__ApiKey="YOUR_API_KEY_HERE"

# Windows
set AlphaVantage__ApiKey=YOUR_API_KEY_HERE

# Docker
docker run -e AlphaVantage__ApiKey="YOUR_API_KEY_HERE" ...
```

**docker-compose.yml**:
```yaml
services:
  api:
    environment:
      - AlphaVantage__ApiKey=${ALPHA_VANTAGE_API_KEY}
```

#### Option 2: Azure Key Vault

For Azure deployments, use Key Vault for maximum security:

```csharp
// Program.cs
builder.Configuration.AddAzureKeyVault(
    new Uri($"https://{keyVaultName}.vault.azure.net/"),
    new DefaultAzureCredential());
```

Store your key in Key Vault:
```bash
az keyvault secret set \
  --vault-name "your-keyvault" \
  --name "AlphaVantage--ApiKey" \
  --value "YOUR_API_KEY_HERE"
```

### API Key Security Best Practices

✅ **DO**:
- Store keys in User Secrets (dev) or Key Vault (prod)
- Use environment variables for containerized deployments
- Rotate keys periodically
- Use different keys for different environments
- Monitor API key usage

❌ **DON'T**:
- Commit API keys to source control
- Share keys between environments
- Log API keys in plain text
- Expose keys in error messages
- Hard-code keys in configuration files

### Obtaining an API Key

1. **Visit Alpha Vantage**: Go to [https://www.alphavantage.co/support/#api-key](https://www.alphavantage.co/support/#api-key)

2. **Sign Up**: Provide your email and basic information

3. **Receive Key**: You'll receive your API key immediately via email

4. **Choose Tier**:
   - **Free**: 25 requests/day, 5 requests/minute
   - **Premium**: Starting at $49.99/month for 500 requests/day

### Validating Your API Key

The system automatically validates your API key on startup. Check the logs:

```
[INFO] Alpha Vantage provider initialized successfully
[INFO] API key validated: av-****1234
```

If validation fails:
```
[ERROR] Alpha Vantage API key validation failed: Invalid API key
```

---

## Rate Limit Management

### Understanding Rate Limits

Alpha Vantage enforces rate limits based on your subscription tier:

| Tier | Daily Limit | Minute Limit | Cost |
|------|-------------|--------------|------|
| Free | 25 requests | 5 requests | $0 |
| Basic | 500 requests | 75 requests | $49.99/mo |
| Pro | 1,200 requests | 150 requests | $149.99/mo |

### How Rate Limiting Works

The system uses a **token bucket algorithm** to enforce rate limits:

1. **Request Made**: System checks if tokens are available
2. **Token Available**: Request proceeds, token consumed
3. **No Tokens**: Request queued or cached data returned
4. **Token Refill**: Tokens automatically refill based on time

### Configuring Rate Limits

Match your configuration to your Alpha Vantage tier:

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

**Premium Tier Example**:
```json
{
  "AlphaVantage": {
    "RateLimit": {
      "RequestsPerMinute": 75,
      "RequestsPerDay": 500
    }
  }
}
```

### Rate Limit Behavior

#### When Rate Limit is Reached

1. **Cache Check**: System checks if cached data is available
2. **Return Cached**: If cache hit, returns cached data with warning
3. **Queue Request**: If no cache, queues request for later
4. **Log Event**: Logs rate limit event for monitoring

#### Monitoring Rate Limits

Check rate limit status via the metrics endpoint:

```bash
GET /api/health/metrics
```

Response:
```json
{
  "providers": {
    "alphaVantage": {
      "rateLimitRemaining": {
        "daily": 18,
        "minute": 3
      },
      "rateLimitResets": {
        "daily": "2025-11-23T00:00:00Z",
        "minute": "2025-11-22T14:35:00Z"
      }
    }
  }
}
```

### Rate Limit Best Practices

#### 1. Enable Aggressive Caching

```json
{
  "Cache": {
    "AlphaVantage": {
      "QuoteTTL": 900,        // 15 minutes
      "HistoricalTTL": 86400, // 24 hours
      "FundamentalsTTL": 21600 // 6 hours
    }
  }
}
```

#### 2. Use Fallback Strategy

Configure Yahoo Finance as fallback to handle overflow:

```json
{
  "DataProvider": {
    "PrimaryProvider": "AlphaVantage",
    "FallbackProvider": "YahooFinance",
    "Strategy": "Fallback"
  }
}
```

#### 3. Batch Requests

Use batch endpoints when fetching multiple symbols:

```csharp
// Instead of multiple calls
var aapl = await GetQuoteAsync("AAPL");
var msft = await GetQuoteAsync("MSFT");
var googl = await GetQuoteAsync("GOOGL");

// Use batch call (1 request instead of 3)
var quotes = await GetQuotesAsync(new[] { "AAPL", "MSFT", "GOOGL" });
```

#### 4. Monitor Usage

Set up alerts for rate limit warnings:

```json
{
  "Monitoring": {
    "RateLimitWarningThreshold": 0.8,  // Alert at 80% usage
    "EnableRateLimitAlerts": true
  }
}
```

#### 5. Schedule Heavy Operations

Run data-intensive operations during off-peak hours:

```csharp
// Schedule historical data fetches at night
// when user traffic is low
```

---

## Cost Optimization Tips

### Understanding Costs

#### Alpha Vantage Pricing

- **Free Tier**: $0/month, 25 requests/day
- **Basic Tier**: $49.99/month, 500 requests/day
- **Pro Tier**: $149.99/month, 1,200 requests/day

#### Cost Per Request

| Tier | Cost per Request |
|------|------------------|
| Free | $0 |
| Basic | ~$0.10 |
| Pro | ~$0.12 |

### Optimization Strategies

#### 1. Maximize Cache Usage

**Impact**: Can reduce API calls by 80-90%

```json
{
  "Cache": {
    "AlphaVantage": {
      "QuoteTTL": 900,        // 15 min for quotes
      "HistoricalTTL": 86400, // 24 hours for historical
      "FundamentalsTTL": 21600, // 6 hours for fundamentals
      "ProfileTTL": 604800    // 7 days for profiles
    }
  }
}
```

**Savings Example**:
- Without cache: 1,000 requests/day = $100/day (Basic tier)
- With cache (90% hit rate): 100 requests/day = $10/day
- **Monthly savings**: ~$2,700

#### 2. Use Cost-Optimized Strategy

**Impact**: Automatically uses free providers when possible

```json
{
  "DataProvider": {
    "PrimaryProvider": "YahooFinance",
    "FallbackProvider": "AlphaVantage",
    "Strategy": "CostOptimized"
  }
}
```

**How it works**:
- Uses Yahoo Finance (free) for most requests
- Falls back to Alpha Vantage only when Yahoo Finance fails
- Tracks costs and optimizes provider selection

#### 3. Enable Data Enrichment Selectively

**Impact**: Reduces API calls for calculated fields

```json
{
  "AlphaVantage": {
    "DataEnrichment": {
      "EnableBidAskEnrichment": true,      // Only if needed
      "EnableCalculatedFields": true,       // Cache 52-week high/low
      "CalculatedFieldsCacheTTL": 86400    // 24-hour cache
    }
  }
}
```

**Savings**:
- Without enrichment: 3 API calls per quote (quote + historical + fundamentals)
- With enrichment: 1 API call + cached calculations
- **Reduction**: 66% fewer API calls

#### 4. Implement Request Deduplication

The system automatically deduplicates concurrent requests for the same symbol:

```csharp
// Multiple concurrent requests for AAPL
// Only 1 API call is made, result shared
Task.WaitAll(
    GetQuoteAsync("AAPL"),
    GetQuoteAsync("AAPL"),
    GetQuoteAsync("AAPL")
);
```

#### 5. Use Appropriate Data Freshness

Match cache TTL to your use case:

| Use Case | Recommended TTL | Rationale |
|----------|----------------|-----------|
| Real-time trading | 1-5 minutes | Need fresh data |
| Portfolio tracking | 15-30 minutes | Balance freshness and cost |
| Historical analysis | 24 hours | Data doesn't change |
| Company profiles | 7 days | Rarely changes |

#### 6. Monitor and Set Cost Limits

```json
{
  "CostTracking": {
    "EnableCostTracking": true,
    "DailyBudget": 100,
    "MonthlyBudget": 2000,
    "AlertThreshold": 0.8,
    "EnforceHardLimit": true
  }
}
```

### Cost Tracking

Monitor your API usage and costs:

```bash
GET /api/health/metrics
```

Response includes cost tracking:
```json
{
  "costTracking": {
    "alphaVantage": {
      "requestsToday": 18,
      "requestsThisMonth": 342,
      "estimatedCostToday": 1.80,
      "estimatedCostThisMonth": 34.20,
      "tier": "Basic",
      "budgetRemaining": 65.80
    }
  }
}
```

### Cost Optimization Checklist

- [ ] Enable aggressive caching with appropriate TTLs
- [ ] Use Cost-Optimized or Fallback strategy
- [ ] Configure data enrichment based on needs
- [ ] Set up cost tracking and alerts
- [ ] Monitor cache hit rates
- [ ] Review and optimize cache TTLs monthly
- [ ] Consider upgrading tier if consistently hitting limits
- [ ] Use batch endpoints for multiple symbols
- [ ] Schedule heavy operations during off-peak hours

### ROI Calculator

**Scenario**: 10,000 quote requests per day

| Configuration | API Calls | Monthly Cost | Savings |
|---------------|-----------|--------------|---------|
| No cache | 10,000/day | $30,000 | - |
| 50% cache hit | 5,000/day | $15,000 | 50% |
| 80% cache hit | 2,000/day | $6,000 | 80% |
| 90% cache hit | 1,000/day | $3,000 | 90% |

**Recommendation**: Aim for 80-90% cache hit rate for optimal cost/performance balance.

---

## Monitoring and Troubleshooting

### Health Check Endpoint

Check provider health status:

```bash
GET /api/health
```

Response:
```json
{
  "status": "Healthy",
  "providers": {
    "alphaVantage": {
      "status": "Healthy",
      "responseTime": "180ms",
      "lastCheck": "2025-11-22T14:30:00Z"
    },
    "yahooFinance": {
      "status": "Unhealthy",
      "responseTime": "5000ms",
      "lastCheck": "2025-11-22T14:30:00Z",
      "error": "Timeout"
    }
  },
  "currentProvider": "alphaVantage",
  "strategy": "Fallback"
}
```

### Metrics Endpoint

Get detailed metrics:

```bash
GET /api/health/metrics
```

### Common Issues and Solutions

#### Issue: "Invalid API Key" Error

**Symptoms**:
```
[ERROR] Alpha Vantage API key validation failed: Invalid API key
```

**Solutions**:
1. Verify API key is correctly set in user secrets or environment variables
2. Check for extra spaces or characters in the key
3. Ensure key hasn't expired or been revoked
4. Request a new key from Alpha Vantage

#### Issue: Rate Limit Exceeded

**Symptoms**:
```
[WARN] Alpha Vantage rate limit exceeded, returning cached data
```

**Solutions**:
1. Enable more aggressive caching
2. Configure fallback provider
3. Upgrade to higher Alpha Vantage tier
4. Use Cost-Optimized strategy

#### Issue: Provider Always Unhealthy

**Symptoms**:
```
[ERROR] Alpha Vantage health check failed: Timeout
```

**Solutions**:
1. Check network connectivity
2. Verify firewall rules allow outbound HTTPS
3. Increase timeout setting
4. Check Alpha Vantage status page

#### Issue: High API Costs

**Symptoms**:
- Monthly bill higher than expected
- Frequent rate limit warnings

**Solutions**:
1. Review cache hit rates (should be >80%)
2. Enable data enrichment to reduce calls
3. Use Cost-Optimized strategy
4. Implement request deduplication
5. Set cost limits and alerts

### Logging

Enable detailed logging for troubleshooting:

```json
{
  "Logging": {
    "LogLevel": {
      "StockSensePro.Infrastructure.Services.AlphaVantageService": "Debug",
      "StockSensePro.Infrastructure.RateLimiting": "Debug"
    }
  }
}
```

### Performance Monitoring

Key metrics to monitor:

1. **Cache Hit Rate**: Should be >80%
2. **Average Response Time**: Should be <500ms
3. **Error Rate**: Should be <1%
4. **Rate Limit Usage**: Should stay below 80% of limit
5. **Cost per Request**: Track trends over time

---

## FAQ

### General Questions

**Q: Can I use multiple providers simultaneously?**

A: Yes! Use the RoundRobin or CostOptimized strategy to distribute requests across multiple providers.

**Q: What happens if my API key expires?**

A: The system will log errors and automatically fall back to the secondary provider if configured. Update your API key in user secrets or environment variables.

**Q: Can I switch providers without restarting the application?**

A: Currently, provider configuration requires an application restart. Dynamic configuration is planned for a future release.

### Configuration Questions

**Q: Which strategy should I use?**

A: For production, use **Fallback** strategy with Alpha Vantage as primary and Yahoo Finance as fallback. This provides the best balance of reliability and cost.

**Q: How do I know which provider is being used?**

A: Check the `/api/health/metrics` endpoint, which shows the current provider and strategy.

**Q: Can I use different providers for different operations?**

A: Not currently. The strategy applies to all operations. This feature is planned for a future release.

### Rate Limiting Questions

**Q: What happens when I hit the rate limit?**

A: The system returns cached data if available, or queues the request for later. If no cache is available and fallback is enabled, it switches to the fallback provider.

**Q: How can I increase my rate limit?**

A: Upgrade your Alpha Vantage subscription tier, or use multiple API keys with the RoundRobin strategy.

**Q: Does caching count against my rate limit?**

A: No! Cache hits don't make API calls, so they don't count against your rate limit.

### Cost Questions

**Q: How much will Alpha Vantage cost me?**

A: It depends on your usage. The free tier (25 requests/day) is sufficient for small applications. With 80% cache hit rate, you can serve 125 user requests per day on the free tier.

**Q: Can I set a hard cost limit?**

A: Yes! Configure `CostTracking.EnforceHardLimit` to automatically stop making API calls when your budget is reached.

**Q: Is Yahoo Finance really free?**

A: Yes, Yahoo Finance doesn't charge for API access, but it's subject to fair use policies and may be less reliable than paid providers.

### Technical Questions

**Q: How is data cached?**

A: Data is cached in Redis with provider-specific keys and TTLs. Cache keys include the provider name, data type, and symbol.

**Q: Can I clear the cache?**

A: Yes, restart Redis or use Redis CLI to clear specific keys. The system will automatically repopulate the cache.

**Q: How do I test my configuration?**

A: Use the Mock provider in development, or make test API calls and check the logs and metrics endpoint.

---

## Support and Resources

### Documentation

- [Alpha Vantage API Documentation](https://www.alphavantage.co/documentation/)
- [Provider Caching Guide](./PROVIDER_CACHING.md)
- [API Key Security Guide](./API_KEY_SECURITY.md)
- [Metrics Endpoint Documentation](./PROVIDER_METRICS_ENDPOINT.md)

### Getting Help

1. **Check Logs**: Review application logs for errors and warnings
2. **Check Metrics**: Use `/api/health/metrics` to diagnose issues
3. **Review Configuration**: Verify all settings are correct
4. **Test Connectivity**: Ensure network access to Alpha Vantage

### Best Practices Summary

✅ **Configuration**
- Use Fallback strategy for production
- Configure appropriate rate limits for your tier
- Enable automatic fallback

✅ **Security**
- Store API keys in User Secrets or Key Vault
- Never commit keys to source control
- Rotate keys periodically

✅ **Performance**
- Enable aggressive caching (80%+ hit rate)
- Use appropriate cache TTLs
- Monitor response times

✅ **Cost**
- Track API usage and costs
- Set budget alerts
- Use Cost-Optimized strategy when appropriate

✅ **Monitoring**
- Check health endpoint regularly
- Monitor rate limit usage
- Set up alerts for errors

---

## Appendix

### Complete Configuration Example

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
      "EnableCalculatedFields": true,
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
  },
  "CostTracking": {
    "EnableCostTracking": true,
    "DailyBudget": 100,
    "MonthlyBudget": 2000,
    "AlertThreshold": 0.8,
    "EnforceHardLimit": false
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "StockSensePro.Infrastructure.Services.AlphaVantageService": "Information"
    }
  }
}
```

### Environment Variables Reference

```bash
# Required
AlphaVantage__ApiKey=your_api_key_here

# Optional (override appsettings.json)
DataProvider__PrimaryProvider=AlphaVantage
DataProvider__FallbackProvider=YahooFinance
DataProvider__Strategy=Fallback
AlphaVantage__Enabled=true
AlphaVantage__RateLimit__RequestsPerDay=25
AlphaVantage__RateLimit__RequestsPerMinute=5
```

### Quick Reference Commands

```bash
# Set API key (development)
dotnet user-secrets set "AlphaVantage:ApiKey" "YOUR_KEY"

# Check health
curl http://localhost:5000/api/health

# Check metrics
curl http://localhost:5000/api/health/metrics

# View logs
docker logs stocksensepro-api

# Clear Redis cache
redis-cli FLUSHALL
```

---

**Last Updated**: November 22, 2025  
**Version**: 1.0  
**Feedback**: Please report issues or suggestions to the development team
