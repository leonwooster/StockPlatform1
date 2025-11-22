# Provider Metrics Endpoint

## Overview

The provider metrics endpoint provides comprehensive monitoring and observability for all configured data providers in the StockSensePro system.

## Endpoint

```
GET /api/health/providers
```

## Response Format

```json
{
  "providers": {
    "YahooFinance": {
      "totalRequests": 1500,
      "successfulRequests": 1450,
      "failedRequests": 50,
      "averageResponseTimeMs": 250.5,
      "isHealthy": true,
      "lastHealthCheck": "2025-11-21T10:30:00Z",
      "consecutiveFailures": 0,
      "rateLimit": null
    },
    "AlphaVantage": {
      "totalRequests": 20,
      "successfulRequests": 20,
      "failedRequests": 0,
      "averageResponseTimeMs": 180.3,
      "isHealthy": true,
      "lastHealthCheck": "2025-11-21T10:30:00Z",
      "consecutiveFailures": 0,
      "rateLimit": {
        "minuteRequestsRemaining": 3,
        "minuteRequestsLimit": 5,
        "minuteWindowResetIn": "00:00:45",
        "dayRequestsRemaining": 5,
        "dayRequestsLimit": 25,
        "dayWindowResetIn": "13:30:00",
        "isRateLimited": false
      }
    }
  },
  "currentProvider": "AlphaVantage",
  "strategy": "Fallback",
  "timestamp": "2025-11-21T10:30:00Z"
}
```

## Response Fields

### Root Level

- **providers**: Dictionary of provider metrics keyed by provider name
- **currentProvider**: The currently active provider (based on most recent successful requests)
- **strategy**: The provider selection strategy being used (e.g., "Primary", "Fallback", "RoundRobin", "CostOptimized")
- **timestamp**: When the metrics were collected

### Provider Metrics

- **totalRequests**: Total number of API calls made to this provider
- **successfulRequests**: Number of successful API calls
- **failedRequests**: Number of failed API calls
- **averageResponseTimeMs**: Average response time in milliseconds
- **isHealthy**: Whether the provider is currently healthy and available
- **lastHealthCheck**: Timestamp of the last health check
- **consecutiveFailures**: Number of consecutive failures (resets on success)
- **rateLimit**: Rate limit information (only for providers with rate limits, like Alpha Vantage)

### Rate Limit Info (Alpha Vantage only)

- **minuteRequestsRemaining**: Requests remaining in the current minute window
- **minuteRequestsLimit**: Maximum requests allowed per minute
- **minuteWindowResetIn**: Time until the minute window resets
- **dayRequestsRemaining**: Requests remaining in the current day window
- **dayRequestsLimit**: Maximum requests allowed per day
- **dayWindowResetIn**: Time until the day window resets
- **isRateLimited**: Whether requests are currently being rate limited

## Use Cases

### 1. Monitoring Provider Health

Check if providers are healthy and responding:

```bash
curl http://localhost:5000/api/health/providers
```

Look at the `isHealthy` field for each provider.

### 2. Tracking API Usage

Monitor how many API calls are being made to each provider:

```bash
curl http://localhost:5000/api/health/providers | jq '.providers[] | {totalRequests, successfulRequests, failedRequests}'
```

### 3. Rate Limit Monitoring

Check Alpha Vantage rate limit status:

```bash
curl http://localhost:5000/api/health/providers | jq '.providers.AlphaVantage.rateLimit'
```

### 4. Performance Analysis

Compare response times across providers:

```bash
curl http://localhost:5000/api/health/providers | jq '.providers[] | {averageResponseTimeMs}'
```

### 5. Strategy Verification

Verify which provider selection strategy is active:

```bash
curl http://localhost:5000/api/health/providers | jq '{currentProvider, strategy}'
```

## Integration with Monitoring Tools

This endpoint can be integrated with monitoring tools like:

- **Prometheus**: Scrape metrics for alerting and dashboards
- **Grafana**: Visualize provider health and performance over time
- **DataDog**: Track API usage and costs
- **Custom Dashboards**: Build real-time monitoring UIs

## Related Services

- **IProviderHealthMonitor**: Tracks health status and response times
- **IProviderMetricsTracker**: Tracks API call counts (success/failure)
- **IAlphaVantageRateLimiter**: Manages Alpha Vantage rate limits
- **IDataProviderStrategy**: Determines provider selection logic

## Notes

- Metrics are tracked in-memory and reset on application restart
- Health checks run periodically based on `DataProvider:HealthCheckIntervalSeconds` configuration
- Rate limit information is only available for providers that implement rate limiting (currently Alpha Vantage)
- The `currentProvider` is determined by the provider with the most successful requests, or the first available provider if no requests have been made
