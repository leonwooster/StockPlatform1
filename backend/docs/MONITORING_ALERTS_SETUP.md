# Alpha Vantage Integration - Monitoring and Alerts Setup

## Table of Contents

1. [Overview](#overview)
2. [Monitoring Strategy](#monitoring-strategy)
3. [Health Check Configuration](#health-check-configuration)
4. [Metrics Collection](#metrics-collection)
5. [Alert Configuration](#alert-configuration)
6. [Dashboard Setup](#dashboard-setup)
7. [Log Aggregation](#log-aggregation)
8. [Performance Monitoring](#performance-monitoring)

---

## Overview

This document provides comprehensive guidance for setting up monitoring and alerts for the Alpha Vantage integration. Proper monitoring ensures early detection of issues and enables proactive response.

### Monitoring Goals

- **Availability**: Ensure 99.9% uptime
- **Performance**: Maintain response times < 500ms
- **Reliability**: Keep error rates < 1%
- **Cost Control**: Stay within budget
- **Early Detection**: Identify issues before users are impacted

### Key Metrics

| Category | Metrics |
|----------|---------|
| **Health** | Provider status, uptime, connectivity |
| **Performance** | Response time (p50, p95, p99), throughput |
| **Reliability** | Error rate, success rate, fallback events |
| **Resources** | Cache hit rate, rate limit usage, API calls |
| **Cost** | Daily cost, monthly cost, budget remaining |

---

## Monitoring Strategy

### Three-Tier Monitoring

```
┌─────────────────────────────────────────────────────────┐
│                  Tier 1: Real-Time                       │
│  - Health checks every 30 seconds                       │
│  - Critical alerts (PagerDuty)                          │
│  - Immediate response required                          │
└─────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────┐
│                  Tier 2: Near Real-Time                  │
│  - Metrics collection every 1-5 minutes                 │
│  - Warning alerts (Slack, Email)                        │
│  - Investigation within 1 hour                          │
└─────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────┐
│                  Tier 3: Historical                      │
│  - Daily/weekly reports                                 │
│  - Trend analysis                                       │
│  - Capacity planning                                    │
└─────────────────────────────────────────────────────────┘
```


### Monitoring Architecture

```
┌─────────────────────────────────────────────────────────┐
│                  Application                             │
│  - Health endpoints                                     │
│  - Metrics endpoints                                    │
│  - Structured logging                                   │
└────────────────┬────────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────────┐
│              Monitoring Tools                            │
│  - Prometheus (metrics)                                 │
│  - Grafana (dashboards)                                 │
│  - ELK Stack (logs)                                     │
│  - PagerDuty (alerts)                                   │
└────────────────┬────────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────────┐
│              Alert Channels                              │
│  - PagerDuty (critical)                                 │
│  - Slack (warnings)                                     │
│  - Email (info)                                         │
└─────────────────────────────────────────────────────────┘
```

---

## Health Check Configuration

### Application Health Endpoint

**Endpoint**: `GET /api/health`

**Response Format**:
```json
{
  "status": "Healthy",
  "timestamp": "2025-11-22T14:30:00Z",
  "providers": {
    "alphaVantage": {
      "status": "Healthy",
      "responseTime": "180ms",
      "lastCheck": "2025-11-22T14:30:00Z",
      "consecutiveFailures": 0
    },
    "yahooFinance": {
      "status": "Healthy",
      "responseTime": "250ms",
      "lastCheck": "2025-11-22T14:30:00Z",
      "consecutiveFailures": 0
    }
  },
  "currentProvider": "alphaVantage",
  "strategy": "Fallback"
}
```

### Health Check Script

```bash
#!/bin/bash
# health-check.sh

ENDPOINT="http://localhost:5000/api/health"
TIMEOUT=5

response=$(curl -s -w "\n%{http_code}" --max-time $TIMEOUT "$ENDPOINT")
http_code=$(echo "$response" | tail -n1)
body=$(echo "$response" | sed '$d')

if [ "$http_code" -eq 200 ]; then
    status=$(echo "$body" | jq -r '.status')
    if [ "$status" = "Healthy" ]; then
        echo "OK: Service is healthy"
        exit 0
    else
        echo "WARNING: Service status is $status"
        exit 1
    fi
else
    echo "CRITICAL: Health check failed with HTTP $http_code"
    exit 2
fi
```


### Systemd Health Check Service

```ini
# /etc/systemd/system/stocksensepro-healthcheck.service
[Unit]
Description=StockSensePro Health Check
After=stocksensepro.service

[Service]
Type=oneshot
ExecStart=/usr/local/bin/health-check.sh
StandardOutput=journal
StandardError=journal

[Install]
WantedBy=multi-user.target
```

```ini
# /etc/systemd/system/stocksensepro-healthcheck.timer
[Unit]
Description=Run StockSensePro Health Check every 30 seconds

[Timer]
OnBootSec=30s
OnUnitActiveSec=30s
AccuracySec=1s

[Install]
WantedBy=timers.target
```

**Enable and start**:
```bash
sudo systemctl enable stocksensepro-healthcheck.timer
sudo systemctl start stocksensepro-healthcheck.timer
```

---

## Metrics Collection

### Prometheus Configuration

**prometheus.yml**:
```yaml
global:
  scrape_interval: 15s
  evaluation_interval: 15s

scrape_configs:
  - job_name: 'stocksensepro'
    static_configs:
      - targets: ['localhost:5000']
    metrics_path: '/api/health/metrics'
    scrape_interval: 60s
```

### Metrics Endpoint

**Endpoint**: `GET /api/health/metrics`

**Response Format**:
```json
{
  "timestamp": "2025-11-22T14:30:00Z",
  "providers": {
    "alphaVantage": {
      "totalRequests": 450,
      "successfulRequests": 445,
      "failedRequests": 5,
      "errorRate": 0.011,
      "averageResponseTime": "180ms",
      "p95ResponseTime": "250ms",
      "p99ResponseTime": "350ms",
      "rateLimitRemaining": {
        "daily": 50,
        "minute": 3
      },
      "rateLimitResets": {
        "daily": "2025-11-23T00:00:00Z",
        "minute": "2025-11-22T14:31:00Z"
      },
      "isHealthy": true,
      "lastHealthCheck": "2025-11-22T14:30:00Z"
    },
    "yahooFinance": {
      "totalRequests": 50,
      "successfulRequests": 48,
      "failedRequests": 2,
      "errorRate": 0.04,
      "averageResponseTime": "250ms",
      "isHealthy": true
    }
  },
  "cache": {
    "hitRate": 0.85,
    "totalHits": 3825,
    "totalMisses": 675,
    "totalRequests": 4500
  },
  "costTracking": {
    "alphaVantage": {
      "requestsToday": 450,
      "requestsThisMonth": 8500,
      "estimatedCostToday": 45.00,
      "estimatedCostThisMonth": 850.00,
      "budgetRemaining": 150.00,
      "tier": "Basic"
    }
  },
  "currentProvider": "alphaVantage",
  "strategy": "Fallback"
}
```


---

## Alert Configuration

### Critical Alerts (PagerDuty)

```yaml
# alerts/critical.yml
groups:
  - name: critical_alerts
    interval: 30s
    rules:
      - alert: ServiceDown
        expr: up{job="stocksensepro"} == 0
        for: 1m
        labels:
          severity: critical
        annotations:
          summary: "StockSensePro service is down"
          description: "Service has been down for more than 1 minute"
          
      - alert: HighErrorRate
        expr: error_rate > 0.05
        for: 5m
        labels:
          severity: critical
        annotations:
          summary: "Error rate above 5%"
          description: "Error rate is {{ $value }}%"
          
      - alert: AllProvidersDown
        expr: healthy_providers_count == 0
        for: 2m
        labels:
          severity: critical
        annotations:
          summary: "All data providers are down"
          description: "No healthy providers available"
```

### Warning Alerts (Slack)

```yaml
# alerts/warnings.yml
groups:
  - name: warning_alerts
    interval: 5m
    rules:
      - alert: ElevatedErrorRate
        expr: error_rate > 0.02
        for: 10m
        labels:
          severity: warning
        annotations:
          summary: "Error rate above 2%"
          
      - alert: HighResponseTime
        expr: p95_response_time > 1000
        for: 10m
        labels:
          severity: warning
        annotations:
          summary: "P95 response time above 1 second"
          
      - alert: LowCacheHitRate
        expr: cache_hit_rate < 0.70
        for: 30m
        labels:
          severity: warning
        annotations:
          summary: "Cache hit rate below 70%"
          
      - alert: RateLimitWarning
        expr: rate_limit_usage > 0.90
        for: 5m
        labels:
          severity: warning
        annotations:
          summary: "Rate limit usage above 90%"
```

### Info Alerts (Email)

```yaml
# alerts/info.yml
groups:
  - name: info_alerts
    interval: 15m
    rules:
      - alert: ProviderFallback
        expr: provider_switched == 1
        labels:
          severity: info
        annotations:
          summary: "Provider switched to fallback"
          
      - alert: CostBudgetWarning
        expr: daily_cost > daily_budget * 0.8
        labels:
          severity: info
        annotations:
          summary: "Daily cost at 80% of budget"
```

---

## Dashboard Setup

### Grafana Dashboard JSON

See `backend/docs/grafana-dashboard.json` for complete dashboard configuration.

### Key Dashboard Panels

1. **System Overview**
   - Current provider
   - Strategy in use
   - Overall health status
   - Uptime percentage

2. **Request Metrics**
   - Requests per minute
   - Error rate over time
   - Response time percentiles
   - Success rate

3. **Provider Health**
   - Provider status indicators
   - Response times by provider
   - Fallback events
   - Consecutive failures

4. **Cache Performance**
   - Hit rate over time
   - Cache size
   - Hits vs misses
   - TTL effectiveness

5. **Rate Limiting**
   - Daily usage gauge
   - Minute usage gauge
   - Time until reset
   - Historical usage

6. **Cost Tracking**
   - Daily cost trend
   - Monthly cost projection
   - Budget remaining
   - Cost per provider

---

## Log Aggregation

### Structured Logging Format

```json
{
  "timestamp": "2025-11-22T14:30:00.123Z",
  "level": "Information",
  "message": "Alpha Vantage API Request",
  "properties": {
    "provider": "AlphaVantage",
    "endpoint": "GLOBAL_QUOTE",
    "symbol": "AAPL",
    "responseTime": 180,
    "statusCode": 200,
    "cacheHit": false
  }
}
```

### ELK Stack Configuration

**Logstash Pipeline**:
```ruby
input {
  file {
    path => "/var/log/stocksensepro/*.log"
    codec => json
  }
}

filter {
  if [properties][provider] {
    mutate {
      add_field => { "provider" => "%{[properties][provider]}" }
    }
  }
}

output {
  elasticsearch {
    hosts => ["localhost:9200"]
    index => "stocksensepro-%{+YYYY.MM.dd}"
  }
}
```

---

## Performance Monitoring

### Application Performance Monitoring (APM)

**Recommended Tools**:
- Application Insights (Azure)
- New Relic
- Datadog
- Elastic APM

### Key Performance Indicators

| KPI | Target | Alert Threshold |
|-----|--------|-----------------|
| Response Time (p50) | < 200ms | > 500ms |
| Response Time (p95) | < 500ms | > 1000ms |
| Response Time (p99) | < 1000ms | > 2000ms |
| Error Rate | < 1% | > 2% |
| Cache Hit Rate | > 80% | < 70% |
| Uptime | > 99.9% | < 99.5% |

---

**Last Updated**: November 22, 2025  
**Version**: 1.0

