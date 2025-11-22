# Alpha Vantage Integration - Migration Guide

## Table of Contents

1. [Overview](#overview)
2. [Migration Strategy](#migration-strategy)
3. [Pre-Migration Assessment](#pre-migration-assessment)
4. [Migration Steps](#migration-steps)
5. [Data Migration](#data-migration)
6. [Configuration Migration](#configuration-migration)
7. [Testing Migration](#testing-migration)
8. [Post-Migration Validation](#post-migration-validation)
9. [Rollback Plan](#rollback-plan)

---

## Overview

This guide helps you migrate from the single-provider architecture (Yahoo Finance only) to the multi-provider architecture with Alpha Vantage integration.

### What's Changing

**Before**:
- Single provider (Yahoo Finance)
- Direct dependency on `IStockDataProvider`
- No provider selection logic
- No rate limiting
- No cost tracking

**After**:
- Multiple providers (Yahoo Finance, Alpha Vantage, Mock)
- Provider selection via strategies
- Factory pattern for provider creation
- Rate limiting and cost tracking
- Health monitoring and automatic fallback

### Migration Timeline

- **Planning**: 1-2 days
- **Code Migration**: 2-3 days
- **Testing**: 2-3 days
- **Staging Deployment**: 1 day
- **Production Deployment**: 1 day
- **Total**: 7-10 days

---

## Migration Strategy

### Approach: Gradual Migration

We recommend a gradual migration approach to minimize risk:

1. **Phase 1**: Deploy new code with Yahoo Finance as primary (no behavior change)
2. **Phase 2**: Enable Alpha Vantage as fallback (safety net)
3. **Phase 3**: Switch to Alpha Vantage as primary (full migration)

### Compatibility

The new architecture is **backward compatible**:
- Existing API endpoints unchanged
- Existing data models unchanged
- Existing cache keys compatible
- No database schema changes required


---

## Pre-Migration Assessment

### Current System Inventory

**Document your current setup**:

```bash
# 1. Check current provider usage
# Review code for IStockDataProvider usage

# 2. Document API call volumes
# Check logs for request counts

# 3. Review cache configuration
# Document current TTLs

# 4. Identify dependencies
# List all services using stock data
```

### Compatibility Check

**Verify compatibility**:

- [ ] .NET 8.0 SDK installed
- [ ] Redis available (for caching)
- [ ] No custom modifications to `IStockDataProvider` interface
- [ ] No hard-coded provider-specific logic
- [ ] Configuration system supports nested sections

### Risk Assessment

**Identify potential risks**:

| Risk | Impact | Mitigation |
|------|--------|------------|
| API key issues | High | Test in staging first |
| Rate limit exceeded | Medium | Start with fallback strategy |
| Cache incompatibility | Low | Cache keys are compatible |
| Performance degradation | Medium | Monitor metrics closely |
| Cost overruns | Medium | Set cost limits and alerts |

---

## Migration Steps

### Step 1: Update Dependencies

**No new dependencies required** - all changes use existing packages.

Verify current packages:
```bash
dotnet list package
```

### Step 2: Update Code

**Option A: Pull from repository**
```bash
git fetch origin
git checkout release/alpha-vantage-integration
git pull origin release/alpha-vantage-integration
```

**Option B: Manual code review**
Review these key files for changes:
- `Program.cs` - DI registration
- `StockService.cs` - Strategy injection
- Configuration files
- New provider implementations


### Step 3: Update Configuration

**Add new configuration sections** to `appsettings.json`:

```json
{
  "DataProvider": {
    "PrimaryProvider": "YahooFinance",
    "FallbackProvider": null,
    "Strategy": "Primary",
    "EnableAutomaticFallback": false,
    "HealthCheckIntervalSeconds": 60
  },
  "AlphaVantage": {
    "ApiKey": "",
    "BaseUrl": "https://www.alphavantage.co/query",
    "Timeout": 10,
    "MaxRetries": 3,
    "Enabled": false,
    "RateLimit": {
      "RequestsPerMinute": 5,
      "RequestsPerDay": 25
    },
    "DataEnrichment": {
      "EnableBidAskEnrichment": false,
      "EnableCalculated52WeekRange": false,
      "EnableCalculatedAverageVolume": false,
      "CalculatedFieldsCacheTTL": 86400
    }
  }
}
```

**Note**: This configuration maintains current behavior (Yahoo Finance only).

### Step 4: Update Dependency Injection

**Verify `Program.cs` includes**:

```csharp
// Provider factory
builder.Services.AddScoped<IStockDataProviderFactory, StockDataProviderFactory>();

// Strategies
builder.Services.AddScoped<PrimaryProviderStrategy>();
builder.Services.AddScoped<FallbackProviderStrategy>();
builder.Services.AddScoped<RoundRobinProviderStrategy>();
builder.Services.AddScoped<CostOptimizedProviderStrategy>();

// Strategy resolver
builder.Services.AddScoped<IDataProviderStrategy>(sp =>
{
    var settings = sp.GetRequiredService<DataProviderSettings>();
    return settings.Strategy switch
    {
        ProviderStrategyType.Primary => sp.GetRequiredService<PrimaryProviderStrategy>(),
        ProviderStrategyType.Fallback => sp.GetRequiredService<FallbackProviderStrategy>(),
        ProviderStrategyType.RoundRobin => sp.GetRequiredService<RoundRobinProviderStrategy>(),
        ProviderStrategyType.CostOptimized => sp.GetRequiredService<CostOptimizedProviderStrategy>(),
        _ => sp.GetRequiredService<PrimaryProviderStrategy>()
    };
});

// Health monitor
builder.Services.AddSingleton<IProviderHealthMonitor, ProviderHealthMonitor>();

// Alpha Vantage provider
builder.Services.AddScoped<AlphaVantageService>();
builder.Services.AddSingleton<IAlphaVantageRateLimiter, AlphaVantageRateLimiter>();
```


### Step 5: Update Service Layer

**StockService changes**:

**Before**:
```csharp
public class StockService
{
    private readonly IStockDataProvider _provider;
    
    public StockService(IStockDataProvider provider)
    {
        _provider = provider;
    }
    
    public async Task<MarketData> GetQuoteAsync(string symbol)
    {
        return await _provider.GetQuoteAsync(symbol);
    }
}
```

**After**:
```csharp
public class StockService
{
    private readonly IDataProviderStrategy _strategy;
    
    public StockService(IDataProviderStrategy strategy)
    {
        _strategy = strategy;
    }
    
    public async Task<MarketData> GetQuoteAsync(string symbol)
    {
        var context = new DataProviderContext
        {
            Symbol = symbol,
            Operation = "GetQuote"
        };
        
        var provider = _strategy.SelectProvider(context);
        return await provider.GetQuoteAsync(symbol);
    }
}
```

### Step 6: Build and Test Locally

```bash
# 1. Build the solution
dotnet build

# 2. Run unit tests
dotnet test

# 3. Start the application
dotnet run --project src/StockSensePro.Api

# 4. Test health endpoint
curl http://localhost:5000/api/health

# 5. Test quote endpoint
curl http://localhost:5000/api/stocks/quote/AAPL

# 6. Verify provider
curl http://localhost:5000/api/health/metrics | jq '.currentProvider'
# Should show "yahooFinance"
```

---

## Data Migration

### Cache Migration

**Good news**: No cache migration needed!

The new architecture uses compatible cache keys:
- Old: `quote:AAPL`
- New: `yahoofinance:quote:AAPL`

Existing cache entries will naturally expire and be replaced.

**Optional**: Clear cache for clean start
```bash
redis-cli FLUSHALL
```


### Database Migration

**No database changes required** - the integration doesn't modify the database schema.

Verify current schema:
```bash
dotnet ef migrations list
```

### Historical Data

**No historical data migration needed** - data models are unchanged.

---

## Configuration Migration

### Phase 1: Deploy with Yahoo Finance (No Change)

**Configuration**:
```json
{
  "DataProvider": {
    "PrimaryProvider": "YahooFinance",
    "Strategy": "Primary"
  },
  "AlphaVantage": {
    "Enabled": false
  }
}
```

**Validation**:
- Application starts successfully
- All endpoints work as before
- No errors in logs
- Performance unchanged

### Phase 2: Enable Alpha Vantage as Fallback

**Prerequisites**:
- Obtain Alpha Vantage API key
- Store key securely (User Secrets or Key Vault)

**Configuration**:
```json
{
  "DataProvider": {
    "PrimaryProvider": "YahooFinance",
    "FallbackProvider": "AlphaVantage",
    "Strategy": "Fallback",
    "EnableAutomaticFallback": true
  },
  "AlphaVantage": {
    "ApiKey": "",
    "Enabled": true,
    "RateLimit": {
      "RequestsPerMinute": 5,
      "RequestsPerDay": 25
    }
  }
}
```

**Set API Key**:
```bash
# Development
dotnet user-secrets set "AlphaVantage:ApiKey" "YOUR_KEY"

# Production
export AlphaVantage__ApiKey="YOUR_KEY"
```

**Validation**:
- Yahoo Finance still primary
- Alpha Vantage available as backup
- Fallback triggers on Yahoo Finance errors
- Monitor logs for fallback events


### Phase 3: Switch to Alpha Vantage as Primary

**Configuration**:
```json
{
  "DataProvider": {
    "PrimaryProvider": "AlphaVantage",
    "FallbackProvider": "YahooFinance",
    "Strategy": "Fallback",
    "EnableAutomaticFallback": true
  },
  "AlphaVantage": {
    "Enabled": true,
    "DataEnrichment": {
      "EnableBidAskEnrichment": true,
      "EnableCalculated52WeekRange": true,
      "EnableCalculatedAverageVolume": true
    }
  }
}
```

**Validation**:
- Alpha Vantage is primary provider
- Yahoo Finance available as backup
- Data enrichment working
- Rate limits respected
- Costs within budget

---

## Testing Migration

### Unit Tests

**Run existing tests**:
```bash
dotnet test --filter Category!=Integration
```

All existing tests should pass without modification.

### Integration Tests

**Test provider switching**:
```bash
# 1. Test with Yahoo Finance
curl http://localhost:5000/api/stocks/quote/AAPL

# 2. Switch to Alpha Vantage
# Update configuration

# 3. Test with Alpha Vantage
curl http://localhost:5000/api/stocks/quote/AAPL

# 4. Compare responses
# Should have same structure, different data source
```

### Fallback Testing

**Simulate provider failure**:
```bash
# 1. Temporarily disable Alpha Vantage
# Set invalid API key or disable in config

# 2. Make request
curl http://localhost:5000/api/stocks/quote/AAPL

# 3. Verify fallback to Yahoo Finance
curl http://localhost:5000/api/health/metrics | jq '.currentProvider'

# 4. Check logs for fallback event
sudo journalctl -u stocksensepro | grep "fallback"
```


### Performance Testing

**Compare performance before and after**:

```bash
# Before migration (Yahoo Finance only)
ab -n 1000 -c 10 http://localhost:5000/api/stocks/quote/AAPL

# After migration (with caching)
ab -n 1000 -c 10 http://localhost:5000/api/stocks/quote/AAPL

# Expected: Similar or better performance due to improved caching
```

### Load Testing

**Test under load**:
```bash
# Use Apache Bench or similar tool
ab -n 10000 -c 100 http://localhost:5000/api/stocks/quote/AAPL

# Monitor:
# - Response times
# - Error rates
# - Cache hit rates
# - Rate limit usage
```

---

## Post-Migration Validation

### Functional Validation

**Test all endpoints**:

```bash
# 1. Quote endpoint
curl http://localhost:5000/api/stocks/quote/AAPL

# 2. Multiple quotes
curl "http://localhost:5000/api/stocks/quotes?symbols=AAPL,MSFT,GOOGL"

# 3. Historical data
curl "http://localhost:5000/api/stocks/AAPL/historical?startDate=2025-01-01&endDate=2025-11-22"

# 4. Search
curl "http://localhost:5000/api/stocks/search?query=Apple"

# 5. Fundamentals
curl http://localhost:5000/api/stocks/AAPL/fundamentals

# 6. Company profile
curl http://localhost:5000/api/stocks/AAPL/profile
```

### Health Validation

```bash
# 1. Health check
curl http://localhost:5000/api/health

# Expected response:
# {
#   "status": "Healthy",
#   "providers": {
#     "alphaVantage": { "status": "Healthy" },
#     "yahooFinance": { "status": "Healthy" }
#   }
# }

# 2. Metrics
curl http://localhost:5000/api/health/metrics

# Verify:
# - Current provider is correct
# - Strategy is correct
# - Rate limits are tracking
# - Cache hit rate is good (>80%)
```


### Performance Validation

**Key metrics to verify**:

| Metric | Target | Validation |
|--------|--------|------------|
| Response Time (p95) | < 500ms | `curl -w "@curl-format.txt"` |
| Error Rate | < 1% | Check logs and metrics |
| Cache Hit Rate | > 80% | Check metrics endpoint |
| Uptime | > 99.9% | Monitor over 24 hours |

### Cost Validation

```bash
# Check cost tracking
curl http://localhost:5000/api/health/metrics | jq '.costTracking'

# Verify:
# - Daily cost within budget
# - Request count as expected
# - No unexpected charges
```

---

## Rollback Plan

### Quick Rollback (Configuration Only)

**If issues arise, quickly revert to Yahoo Finance**:

```bash
# 1. Update configuration
export DataProvider__PrimaryProvider="YahooFinance"
export AlphaVantage__Enabled="false"

# 2. Restart application
sudo systemctl restart stocksensepro

# 3. Verify
curl http://localhost:5000/api/health/metrics | jq '.currentProvider'
# Should show "yahooFinance"
```

### Full Rollback (Code)

**If new code has issues**:

```bash
# 1. Stop application
sudo systemctl stop stocksensepro

# 2. Restore from backup
cd /var/www
sudo rm -rf stocksensepro
sudo cp -r stocksensepro-backup-YYYYMMDD stocksensepro

# 3. Start application
sudo systemctl start stocksensepro

# 4. Verify
curl http://localhost:5000/api/health
```

### Rollback Decision Matrix

| Issue | Severity | Action |
|-------|----------|--------|
| Configuration error | Low | Fix configuration, restart |
| High error rate (2-5%) | Medium | Switch to Yahoo Finance |
| High error rate (>5%) | High | Full rollback |
| Performance degradation | Medium | Optimize or rollback |
| Cost overrun | Medium | Switch to Yahoo Finance |
| Data corruption | Critical | Full rollback immediately |


---

## Migration Checklist

### Pre-Migration

- [ ] Current system documented
- [ ] Compatibility verified
- [ ] Risks identified and mitigated
- [ ] Team trained on new architecture
- [ ] Backup created
- [ ] Rollback plan documented

### Migration

- [ ] Code updated
- [ ] Configuration updated
- [ ] Dependencies verified
- [ ] Unit tests passing
- [ ] Integration tests passing
- [ ] Local testing completed

### Phase 1: Deploy with Yahoo Finance

- [ ] Deployed to staging
- [ ] Health check passes
- [ ] All endpoints working
- [ ] Performance validated
- [ ] Monitored for 24 hours
- [ ] Deployed to production

### Phase 2: Enable Alpha Vantage Fallback

- [ ] API key obtained
- [ ] API key stored securely
- [ ] Configuration updated
- [ ] Deployed to staging
- [ ] Fallback tested
- [ ] Monitored for 24 hours
- [ ] Deployed to production

### Phase 3: Switch to Alpha Vantage Primary

- [ ] Configuration updated
- [ ] Deployed to staging
- [ ] Data enrichment verified
- [ ] Rate limits verified
- [ ] Costs verified
- [ ] Monitored for 48 hours
- [ ] Deployed to production

### Post-Migration

- [ ] All endpoints validated
- [ ] Performance metrics good
- [ ] Error rates acceptable
- [ ] Cache hit rate good
- [ ] Costs within budget
- [ ] Monitoring and alerts working
- [ ] Documentation updated
- [ ] Team notified of completion

---

## Troubleshooting Migration Issues

### Issue: Application Won't Start After Migration

**Symptoms**:
```
Application failed to start
Unable to resolve service for type 'IDataProviderStrategy'
```

**Solution**:
1. Verify all services registered in `Program.cs`
2. Check configuration file syntax
3. Ensure strategy is configured correctly


### Issue: Tests Failing After Migration

**Symptoms**:
- Unit tests failing
- Integration tests failing

**Solution**:
1. Update test mocks to use `IDataProviderStrategy`
2. Verify test configuration includes new sections
3. Check for hard-coded provider references

### Issue: Performance Degradation

**Symptoms**:
- Slower response times
- Higher latency

**Solution**:
1. Check cache hit rate (should be >80%)
2. Verify Redis is running and accessible
3. Review cache TTL configuration
4. Check network connectivity to providers

### Issue: Unexpected Costs

**Symptoms**:
- Higher than expected API usage
- Budget alerts triggering

**Solution**:
1. Check cache hit rate
2. Review request patterns
3. Enable data enrichment to reduce calls
4. Consider switching to Cost-Optimized strategy

---

## Best Practices

### During Migration

1. **Test Thoroughly**: Test each phase in staging before production
2. **Monitor Closely**: Watch metrics and logs during and after migration
3. **Communicate**: Keep stakeholders informed of progress
4. **Document**: Record any issues and solutions
5. **Be Patient**: Don't rush - take time to validate each phase

### After Migration

1. **Monitor Continuously**: Watch metrics for at least one week
2. **Optimize**: Adjust cache TTLs and strategies based on usage
3. **Review Costs**: Track costs and optimize as needed
4. **Update Documentation**: Document any customizations or learnings
5. **Train Team**: Ensure team understands new architecture

---

## Support and Resources

### Documentation

- [Deployment Guide](./DEPLOYMENT_GUIDE.md)
- [User Guide](./ALPHA_VANTAGE_USER_GUIDE.md)
- [Developer Guide](./ALPHA_VANTAGE_DEVELOPER_GUIDE.md)
- [Rollback Procedures](./ROLLBACK_PROCEDURES.md)

### Getting Help

1. Check logs for error messages
2. Review metrics endpoint for insights
3. Consult troubleshooting guides
4. Contact development team
5. Reach out to Alpha Vantage support if needed

---

**Last Updated**: November 22, 2025  
**Version**: 1.0  
**Next Review**: December 22, 2025

