# Alpha Vantage Integration - Deployment Guide

## Table of Contents

1. [Overview](#overview)
2. [Pre-Deployment Checklist](#pre-deployment-checklist)
3. [Environment Setup](#environment-setup)
4. [Deployment Steps](#deployment-steps)
5. [Post-Deployment Verification](#post-deployment-verification)
6. [Rollback Procedures](#rollback-procedures)
7. [Monitoring and Alerts](#monitoring-and-alerts)
8. [Troubleshooting](#troubleshooting)

---

## Overview

This guide provides step-by-step instructions for deploying the Alpha Vantage integration to staging and production environments. The deployment includes multi-provider support, rate limiting, caching, health monitoring, and cost tracking.

### Deployment Strategy

- **Zero-downtime deployment**: Rolling updates with health checks
- **Gradual rollout**: Enable Alpha Vantage incrementally
- **Automatic fallback**: Yahoo Finance as backup provider
- **Monitoring**: Real-time metrics and alerts

### Key Components

- Multi-provider architecture (Yahoo Finance, Alpha Vantage, Mock)
- Provider selection strategies (Primary, Fallback, RoundRobin, CostOptimized)
- Rate limiting and cost tracking
- Health monitoring and automatic failover
- Redis caching for performance optimization

---

## Pre-Deployment Checklist

### Code Readiness

- [ ] All unit tests passing (run `dotnet test`)
- [ ] Integration tests completed
- [ ] Code review approved
- [ ] Documentation updated
- [ ] Configuration validated

### Infrastructure Readiness

- [ ] Redis server available and accessible
- [ ] Database migrations applied
- [ ] SSL certificates valid
- [ ] Network connectivity verified
- [ ] Firewall rules configured

### Configuration Readiness

- [ ] Alpha Vantage API key obtained
- [ ] API key stored securely (Key Vault/Environment Variables)
- [ ] Configuration files prepared for each environment
- [ ] Rate limits configured correctly
- [ ] Cache TTLs optimized

### Monitoring Readiness

- [ ] Health check endpoints tested
- [ ] Metrics endpoint verified
- [ ] Logging configured
- [ ] Alerts configured
- [ ] Dashboard created

### Team Readiness

- [ ] Deployment plan reviewed
- [ ] Rollback plan documented
- [ ] On-call team notified
- [ ] Communication plan established
- [ ] Stakeholders informed

---

## Environment Setup

### Development Environment

**Purpose**: Local development and testing

**Configuration** (`appsettings.Development.json`):
```json
{
  "DataProvider": {
    "PrimaryProvider": "Mock",
    "Strategy": "Primary",
    "EnableAutomaticFallback": false
  },
  "AlphaVantage": {
    "Enabled": false
  }
}
```

**API Key Storage**: User Secrets
```bash
dotnet user-secrets set "AlphaVantage:ApiKey" "YOUR_DEV_KEY"
```


### Staging Environment

**Purpose**: Pre-production testing and validation

**Configuration** (`appsettings.Staging.json`):
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
    "Enabled": true,
    "BaseUrl": "https://www.alphavantage.co/query",
    "Timeout": 10,
    "MaxRetries": 3,
    "RateLimit": {
      "RequestsPerMinute": 5,
      "RequestsPerDay": 25
    }
  }
}
```

**API Key Storage**: Environment Variables
```bash
export AlphaVantage__ApiKey="YOUR_STAGING_KEY"
```

### Production Environment

**Purpose**: Live production system

**Configuration** (`appsettings.Production.json`):
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
    "Enabled": true,
    "BaseUrl": "https://www.alphavantage.co/query",
    "Timeout": 10,
    "MaxRetries": 3,
    "RateLimit": {
      "RequestsPerMinute": 75,
      "RequestsPerDay": 500
    },
    "DataEnrichment": {
      "EnableBidAskEnrichment": true,
      "EnableCalculated52WeekRange": true,
      "EnableCalculatedAverageVolume": true
    }
  }
}
```

**API Key Storage**: Azure Key Vault (recommended)
```bash
az keyvault secret set \
  --vault-name "stocksensepro-prod-kv" \
  --name "AlphaVantage--ApiKey" \
  --value "YOUR_PRODUCTION_KEY"
```


---

## Deployment Steps

### Phase 1: Staging Deployment

#### Step 1: Prepare Staging Environment

```bash
# 1. Connect to staging server
ssh user@staging-server

# 2. Navigate to application directory
cd /var/www/stocksensepro

# 3. Backup current deployment
sudo cp -r . ../stocksensepro-backup-$(date +%Y%m%d-%H%M%S)

# 4. Stop the application
sudo systemctl stop stocksensepro
```

#### Step 2: Deploy Code

```bash
# 1. Pull latest code
git fetch origin
git checkout release/alpha-vantage-integration
git pull origin release/alpha-vantage-integration

# 2. Restore dependencies
dotnet restore

# 3. Build application
dotnet build --configuration Release

# 4. Publish application
dotnet publish -c Release -o /var/www/stocksensepro/publish
```

#### Step 3: Configure Environment

```bash
# 1. Set environment variables
sudo nano /etc/systemd/system/stocksensepro.service

# Add these environment variables:
Environment="ASPNETCORE_ENVIRONMENT=Staging"
Environment="AlphaVantage__ApiKey=YOUR_STAGING_KEY"
Environment="ConnectionStrings__Redis=localhost:6379"

# 2. Reload systemd
sudo systemctl daemon-reload
```

#### Step 4: Verify Configuration

```bash
# 1. Check configuration files
cat appsettings.Staging.json

# 2. Verify API key is set (should not show actual key)
dotnet run --no-build --configuration Release -- --check-config

# 3. Test Redis connection
redis-cli ping
```


#### Step 5: Start Application

```bash
# 1. Start the application
sudo systemctl start stocksensepro

# 2. Check status
sudo systemctl status stocksensepro

# 3. Monitor logs
sudo journalctl -u stocksensepro -f
```

#### Step 6: Verify Deployment

```bash
# 1. Check health endpoint
curl http://localhost:5000/api/health

# Expected response:
# {
#   "status": "Healthy",
#   "providers": {
#     "alphaVantage": { "status": "Healthy" },
#     "yahooFinance": { "status": "Healthy" }
#   }
# }

# 2. Check metrics endpoint
curl http://localhost:5000/api/health/metrics

# 3. Test quote endpoint
curl http://localhost:5000/api/stocks/quote/AAPL

# 4. Verify provider is Alpha Vantage
curl http://localhost:5000/api/health/metrics | jq '.currentProvider'
```

#### Step 7: Run Smoke Tests

```bash
# 1. Test multiple symbols
curl http://localhost:5000/api/stocks/quotes?symbols=AAPL,MSFT,GOOGL

# 2. Test historical data
curl "http://localhost:5000/api/stocks/AAPL/historical?startDate=2025-01-01&endDate=2025-11-22"

# 3. Test search
curl "http://localhost:5000/api/stocks/search?query=Apple"

# 4. Test fallback (temporarily disable Alpha Vantage)
# Verify Yahoo Finance takes over

# 5. Monitor logs for errors
sudo journalctl -u stocksensepro --since "5 minutes ago" | grep ERROR
```


### Phase 2: Production Deployment

#### Step 1: Pre-Deployment Validation

```bash
# 1. Verify staging has been stable for 24+ hours
# 2. Review staging metrics and logs
# 3. Confirm no critical issues
# 4. Get approval from stakeholders
```

#### Step 2: Prepare Production Environment

```bash
# 1. Schedule maintenance window (if needed)
# 2. Notify users of deployment
# 3. Backup production database
pg_dump stocksensepro > backup-$(date +%Y%m%d-%H%M%S).sql

# 4. Backup Redis data
redis-cli SAVE
cp /var/lib/redis/dump.rdb /backup/redis-$(date +%Y%m%d-%H%M%S).rdb
```

#### Step 3: Deploy to Production (Blue-Green Deployment)

```bash
# 1. Deploy to "green" environment (new version)
# Keep "blue" environment (current version) running

# 2. Update load balancer to route 10% traffic to green
# Monitor for 15 minutes

# 3. Gradually increase traffic: 25%, 50%, 75%, 100%
# Monitor metrics at each step

# 4. Once 100% traffic on green, keep blue running for 1 hour
# Then decommission blue environment
```

#### Step 4: Enable Alpha Vantage Gradually

**Option A: Start with Fallback Strategy (Recommended)**

```json
{
  "DataProvider": {
    "PrimaryProvider": "YahooFinance",
    "FallbackProvider": "AlphaVantage",
    "Strategy": "Fallback"
  }
}
```

Monitor for 24 hours, then switch:

```json
{
  "DataProvider": {
    "PrimaryProvider": "AlphaVantage",
    "FallbackProvider": "YahooFinance",
    "Strategy": "Fallback"
  }
}
```

**Option B: Use Feature Flag**

```json
{
  "FeatureFlags": {
    "AlphaVantageEnabled": true,
    "AlphaVantageTrafficPercentage": 10
  }
}
```

Gradually increase percentage: 10% → 25% → 50% → 100%


---

## Post-Deployment Verification

### Immediate Checks (0-15 minutes)

```bash
# 1. Application is running
systemctl status stocksensepro

# 2. Health check passes
curl http://localhost:5000/api/health

# 3. No errors in logs
journalctl -u stocksensepro --since "15 minutes ago" | grep ERROR

# 4. Redis connection working
redis-cli KEYS "alphavantage:*" | head -5

# 5. Alpha Vantage API responding
curl http://localhost:5000/api/stocks/quote/AAPL
```

### Short-term Monitoring (15 minutes - 1 hour)

```bash
# 1. Monitor error rates
# Should be < 1%

# 2. Monitor response times
# Should be < 500ms for quotes

# 3. Check cache hit rate
# Should be > 80%

# 4. Verify provider selection
curl http://localhost:5000/api/health/metrics | jq '.currentProvider'

# 5. Monitor rate limit usage
curl http://localhost:5000/api/health/metrics | jq '.providers.alphaVantage.rateLimitRemaining'
```

### Medium-term Monitoring (1-24 hours)

```bash
# 1. Review daily metrics
# - Total requests
# - Provider distribution
# - Error rates
# - Response times
# - Cache hit rates

# 2. Check cost tracking
curl http://localhost:5000/api/health/metrics | jq '.costTracking'

# 3. Verify fallback behavior
# Simulate Alpha Vantage failure and verify Yahoo Finance takes over

# 4. Review logs for warnings
journalctl -u stocksensepro --since "24 hours ago" | grep WARN

# 5. Check for rate limit events
journalctl -u stocksensepro --since "24 hours ago" | grep "rate limit"
```


### Long-term Monitoring (24+ hours)

**Key Metrics to Track**:

| Metric | Target | Alert Threshold |
|--------|--------|-----------------|
| Uptime | 99.9% | < 99.5% |
| Error Rate | < 1% | > 2% |
| Response Time (p95) | < 500ms | > 1000ms |
| Cache Hit Rate | > 80% | < 70% |
| Rate Limit Usage | < 80% | > 90% |
| Daily Cost | Within budget | > 110% of budget |

**Weekly Review**:
- Review cost trends
- Optimize cache TTLs if needed
- Adjust rate limits based on usage
- Review and address any recurring errors
- Update documentation based on learnings

---

## Rollback Procedures

### When to Rollback

Rollback immediately if:
- Error rate > 5%
- Critical functionality broken
- Data corruption detected
- Security vulnerability discovered
- Performance degradation > 50%

### Rollback Decision Matrix

| Issue | Severity | Action |
|-------|----------|--------|
| Error rate 2-5% | Medium | Investigate, prepare rollback |
| Error rate > 5% | High | Rollback immediately |
| Response time +50% | Medium | Switch to fallback provider |
| Response time +100% | High | Rollback immediately |
| Rate limit exceeded | Low | Switch to fallback provider |
| API key invalid | High | Fix configuration or rollback |
| Cache failures | Medium | Investigate, may continue |
| Provider unavailable | Low | Automatic fallback handles it |


### Quick Rollback (Configuration Only)

**Scenario**: Alpha Vantage causing issues, but code is stable

```bash
# 1. Switch back to Yahoo Finance as primary
# Edit appsettings.Production.json or set environment variable
export DataProvider__PrimaryProvider="YahooFinance"

# 2. Restart application
sudo systemctl restart stocksensepro

# 3. Verify
curl http://localhost:5000/api/health/metrics | jq '.currentProvider'
# Should show "yahooFinance"

# 4. Monitor for 15 minutes
# Verify error rates return to normal
```

### Full Rollback (Code Deployment)

**Scenario**: New code has critical issues

```bash
# 1. Stop current application
sudo systemctl stop stocksensepro

# 2. Restore from backup
cd /var/www
sudo rm -rf stocksensepro
sudo cp -r stocksensepro-backup-YYYYMMDD-HHMMSS stocksensepro

# 3. Restore configuration
sudo cp /backup/appsettings.Production.json /var/www/stocksensepro/

# 4. Start application
sudo systemctl start stocksensepro

# 5. Verify
curl http://localhost:5000/api/health
sudo journalctl -u stocksensepro -f
```

### Database Rollback

**Scenario**: Database migrations need to be reverted

```bash
# 1. Stop application
sudo systemctl stop stocksensepro

# 2. Restore database from backup
psql stocksensepro < backup-YYYYMMDD-HHMMSS.sql

# 3. Verify database state
psql stocksensepro -c "SELECT version FROM __EFMigrationsHistory ORDER BY version DESC LIMIT 1;"

# 4. Start application
sudo systemctl start stocksensepro
```


### Blue-Green Rollback

**Scenario**: Using blue-green deployment

```bash
# 1. Update load balancer to route 100% traffic back to blue (old version)
# This is instant - no downtime

# 2. Monitor for 15 minutes
# Verify error rates return to normal

# 3. Keep green environment running for investigation
# Don't decommission until root cause is found

# 4. Once issue is resolved, redeploy to green
```

### Post-Rollback Actions

```bash
# 1. Document the issue
# - What went wrong
# - When it was detected
# - Impact on users
# - Rollback steps taken

# 2. Notify stakeholders
# - Send status update
# - Provide timeline for fix
# - Explain impact

# 3. Root cause analysis
# - Investigate logs
# - Review metrics
# - Identify root cause
# - Document findings

# 4. Create action items
# - Fix the issue
# - Add tests to prevent recurrence
# - Update deployment procedures
# - Schedule re-deployment
```

---

## Monitoring and Alerts

### Health Check Monitoring

**Endpoint**: `GET /api/health`

**Check Frequency**: Every 30 seconds

**Alert Conditions**:
- Status is not "Healthy"
- Response time > 5 seconds
- Endpoint unreachable

**Alert Actions**:
- Send notification to on-call team
- Trigger automatic fallback if configured
- Log incident for review


### Metrics Monitoring

**Endpoint**: `GET /api/health/metrics`

**Check Frequency**: Every 5 minutes

**Key Metrics**:

```json
{
  "providers": {
    "alphaVantage": {
      "totalRequests": 450,
      "successfulRequests": 445,
      "failedRequests": 5,
      "averageResponseTime": "180ms",
      "rateLimitRemaining": {
        "daily": 50,
        "minute": 3
      },
      "isHealthy": true
    }
  },
  "cache": {
    "hitRate": 0.85,
    "totalHits": 3825,
    "totalMisses": 675
  },
  "costTracking": {
    "alphaVantage": {
      "requestsToday": 450,
      "estimatedCostToday": 45.00,
      "budgetRemaining": 55.00
    }
  }
}
```

### Alert Configuration

#### Critical Alerts (Immediate Response)

```yaml
alerts:
  - name: "API Down"
    condition: "health_status != 'Healthy'"
    severity: "critical"
    notification: "pagerduty, slack, email"
    
  - name: "High Error Rate"
    condition: "error_rate > 5%"
    severity: "critical"
    notification: "pagerduty, slack"
    
  - name: "All Providers Down"
    condition: "all_providers_unhealthy == true"
    severity: "critical"
    notification: "pagerduty, slack, email, sms"
```

#### Warning Alerts (Monitor and Investigate)

```yaml
alerts:
  - name: "Elevated Error Rate"
    condition: "error_rate > 2%"
    severity: "warning"
    notification: "slack, email"
    
  - name: "High Response Time"
    condition: "p95_response_time > 1000ms"
    severity: "warning"
    notification: "slack"
    
  - name: "Low Cache Hit Rate"
    condition: "cache_hit_rate < 70%"
    severity: "warning"
    notification: "slack"
    
  - name: "Rate Limit Warning"
    condition: "rate_limit_usage > 90%"
    severity: "warning"
    notification: "slack, email"
    
  - name: "Cost Budget Warning"
    condition: "daily_cost > budget * 0.8"
    severity: "warning"
    notification: "email"
```


#### Info Alerts (Informational Only)

```yaml
alerts:
  - name: "Provider Fallback"
    condition: "provider_switched == true"
    severity: "info"
    notification: "slack"
    
  - name: "Rate Limit Reached"
    condition: "rate_limit_exceeded == true"
    severity: "info"
    notification: "slack"
    
  - name: "Cache Miss Spike"
    condition: "cache_miss_rate > 30%"
    severity: "info"
    notification: "slack"
```

### Logging Configuration

**Production Logging** (`appsettings.Production.json`):

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information",
      "StockSensePro.Infrastructure.Services.AlphaVantageService": "Information",
      "StockSensePro.Application.Strategies": "Information",
      "StockSensePro.Infrastructure.RateLimiting": "Warning"
    }
  },
  "Serilog": {
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
          "outputTemplate": "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"
        }
      },
      {
        "Name": "File",
        "Args": {
          "path": "/var/log/stocksensepro/log-.txt",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 30
        }
      }
    ]
  }
}
```

### Dashboard Setup

**Recommended Metrics Dashboard**:

1. **System Health**
   - Overall health status
   - Uptime percentage
   - Active provider

2. **Request Metrics**
   - Requests per minute
   - Error rate
   - Response time (p50, p95, p99)

3. **Provider Metrics**
   - Requests per provider
   - Provider health status
   - Fallback events

4. **Cache Metrics**
   - Hit rate
   - Miss rate
   - Cache size

5. **Rate Limiting**
   - Daily usage
   - Minute usage
   - Time until reset

6. **Cost Tracking**
   - Daily cost
   - Monthly cost
   - Budget remaining


---

## Troubleshooting

### Issue: Deployment Fails to Start

**Symptoms**:
```
Application failed to start
System.InvalidOperationException: Unable to resolve service
```

**Diagnosis**:
```bash
# Check logs
sudo journalctl -u stocksensepro -n 100

# Check configuration
cat appsettings.Production.json

# Verify dependencies
dotnet --list-runtimes
```

**Solutions**:
1. Verify all dependencies are registered in `Program.cs`
2. Check configuration file syntax (valid JSON)
3. Ensure API key is set correctly
4. Verify Redis is running and accessible

### Issue: High Error Rate After Deployment

**Symptoms**:
- Error rate > 5%
- Multiple failed requests in logs

**Diagnosis**:
```bash
# Check error logs
sudo journalctl -u stocksensepro | grep ERROR | tail -50

# Check provider health
curl http://localhost:5000/api/health

# Check metrics
curl http://localhost:5000/api/health/metrics
```

**Solutions**:
1. If Alpha Vantage errors: Switch to Yahoo Finance
2. If rate limit errors: Increase cache TTLs
3. If network errors: Check firewall rules
4. If authentication errors: Verify API key

### Issue: Slow Response Times

**Symptoms**:
- Response time > 1000ms
- Timeout errors

**Diagnosis**:
```bash
# Check response times
curl -w "@curl-format.txt" -o /dev/null -s http://localhost:5000/api/stocks/quote/AAPL

# Check cache hit rate
curl http://localhost:5000/api/health/metrics | jq '.cache.hitRate'

# Check Redis latency
redis-cli --latency
```

**Solutions**:
1. Increase cache TTLs
2. Check Redis performance
3. Verify network connectivity
4. Consider using RoundRobin strategy to distribute load


### Issue: Rate Limits Exceeded

**Symptoms**:
```
[WARN] Alpha Vantage rate limit exceeded, returning cached data
```

**Diagnosis**:
```bash
# Check rate limit status
curl http://localhost:5000/api/health/metrics | jq '.providers.alphaVantage.rateLimitRemaining'

# Check request volume
curl http://localhost:5000/api/health/metrics | jq '.providers.alphaVantage.totalRequests'
```

**Solutions**:
1. Increase cache TTLs to reduce API calls
2. Enable data enrichment to reduce duplicate calls
3. Switch to Cost-Optimized strategy
4. Upgrade Alpha Vantage tier
5. Use Yahoo Finance as primary temporarily

### Issue: Cache Not Working

**Symptoms**:
- Cache hit rate < 50%
- Every request hits API

**Diagnosis**:
```bash
# Check Redis connection
redis-cli ping

# Check cache keys
redis-cli KEYS "alphavantage:*"

# Check cache TTLs
redis-cli TTL "alphavantage:quote:AAPL"

# Check application logs
sudo journalctl -u stocksensepro | grep "Cache"
```

**Solutions**:
1. Verify Redis is running: `sudo systemctl status redis`
2. Check Redis connection string in configuration
3. Verify cache service is injected in providers
4. Check for cache serialization errors in logs

### Issue: Provider Fallback Not Working

**Symptoms**:
- Requests fail instead of falling back
- Yahoo Finance not being used

**Diagnosis**:
```bash
# Check strategy configuration
curl http://localhost:5000/api/health/metrics | jq '.strategy'

# Check provider health
curl http://localhost:5000/api/health | jq '.providers'

# Check logs for fallback events
sudo journalctl -u stocksensepro | grep "fallback"
```

**Solutions**:
1. Verify `EnableAutomaticFallback` is true
2. Check that `FallbackProvider` is configured
3. Verify Yahoo Finance provider is registered
4. Check exception handling in StockService


### Issue: High Costs

**Symptoms**:
- Monthly bill higher than expected
- Budget alerts triggering

**Diagnosis**:
```bash
# Check cost tracking
curl http://localhost:5000/api/health/metrics | jq '.costTracking'

# Check request volume
curl http://localhost:5000/api/health/metrics | jq '.providers.alphaVantage.totalRequests'

# Check cache hit rate
curl http://localhost:5000/api/health/metrics | jq '.cache.hitRate'
```

**Solutions**:
1. Increase cache TTLs (target 80%+ hit rate)
2. Switch to Cost-Optimized strategy
3. Enable data enrichment to reduce API calls
4. Review and optimize frequently requested symbols
5. Consider using Yahoo Finance for non-critical requests

---

## Appendix

### Deployment Checklist

**Pre-Deployment**:
- [ ] All tests passing
- [ ] Code review completed
- [ ] Configuration validated
- [ ] API key obtained and stored securely
- [ ] Redis available
- [ ] Backup created
- [ ] Rollback plan documented
- [ ] Team notified

**Deployment**:
- [ ] Code deployed
- [ ] Configuration updated
- [ ] Application started
- [ ] Health check passes
- [ ] Smoke tests completed
- [ ] Logs reviewed

**Post-Deployment**:
- [ ] Monitoring enabled
- [ ] Alerts configured
- [ ] Metrics reviewed
- [ ] Performance validated
- [ ] Cost tracking verified
- [ ] Documentation updated
- [ ] Team notified of completion


### Environment Variables Reference

```bash
# Required
ASPNETCORE_ENVIRONMENT=Production
AlphaVantage__ApiKey=your_api_key_here

# Optional (override appsettings.json)
DataProvider__PrimaryProvider=AlphaVantage
DataProvider__FallbackProvider=YahooFinance
DataProvider__Strategy=Fallback
DataProvider__EnableAutomaticFallback=true
AlphaVantage__Enabled=true
AlphaVantage__BaseUrl=https://www.alphavantage.co/query
AlphaVantage__Timeout=10
AlphaVantage__MaxRetries=3
AlphaVantage__RateLimit__RequestsPerDay=500
AlphaVantage__RateLimit__RequestsPerMinute=75

# Database
ConnectionStrings__DefaultConnection=your_db_connection_string
ConnectionStrings__Redis=localhost:6379

# Logging
Logging__LogLevel__Default=Information
```

### Quick Commands Reference

```bash
# Deployment
sudo systemctl stop stocksensepro
sudo systemctl start stocksensepro
sudo systemctl restart stocksensepro
sudo systemctl status stocksensepro

# Logs
sudo journalctl -u stocksensepro -f
sudo journalctl -u stocksensepro --since "1 hour ago"
sudo journalctl -u stocksensepro | grep ERROR

# Health Checks
curl http://localhost:5000/api/health
curl http://localhost:5000/api/health/metrics

# Redis
redis-cli ping
redis-cli KEYS "alphavantage:*"
redis-cli FLUSHALL

# Backup
pg_dump stocksensepro > backup-$(date +%Y%m%d-%H%M%S).sql
redis-cli SAVE
```

### Contact Information

**On-Call Team**:
- Primary: [Contact Info]
- Secondary: [Contact Info]
- Escalation: [Contact Info]

**Stakeholders**:
- Product Owner: [Contact Info]
- Engineering Manager: [Contact Info]
- DevOps Lead: [Contact Info]

**External Support**:
- Alpha Vantage Support: https://www.alphavantage.co/support/
- Redis Support: [Contact Info]
- Cloud Provider Support: [Contact Info]

---

**Last Updated**: November 22, 2025  
**Version**: 1.0  
**Next Review**: December 22, 2025

