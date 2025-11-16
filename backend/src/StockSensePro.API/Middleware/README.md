# Rate Limiting Middleware

## Overview

The Rate Limiting Middleware implements a token bucket algorithm to prevent API abuse and ensure fair usage of the Yahoo Finance API integration. It tracks requests per endpoint across multiple time windows (minute, hour, day) and returns 429 (Too Many Requests) when limits are exceeded.

## Components

### RateLimitMiddleware

The main middleware class that intercepts HTTP requests and applies rate limiting logic.

**Features:**
- Token bucket algorithm for smooth rate limiting
- Per-endpoint tracking (quote, historical, fundamentals, profile, search)
- Multiple time windows (minute, hour, day)
- Automatic retry-after headers
- Rate limit metrics exposure

**Configuration:**

Rate limits are configured in `appsettings.json`:

```json
{
  "YahooFinance": {
    "RateLimit": {
      "RequestsPerMinute": 100,
      "RequestsPerHour": 2000,
      "RequestsPerDay": 20000
    }
  }
}
```

### TokenBucket

Implements the token bucket algorithm for rate limiting.

**How it works:**
1. Each bucket has a capacity (max tokens)
2. Tokens are consumed on each request
3. Buckets automatically refill after the time window expires
4. If no tokens are available, the request is rejected

### RateLimitMetrics

Tracks rate limiting statistics for monitoring and observability.

**Metrics tracked:**
- Total requests per endpoint
- Rate limit hits per endpoint
- Uptime
- Hit rate percentage

**Access metrics:**
```
GET /api/health/metrics
```

## Usage

The middleware is automatically applied to all `/api/stocks/*` endpoints.

### Response Headers

Successful requests include rate limit information:

```
X-RateLimit-Limit-Minute: 100
X-RateLimit-Remaining-Minute: 95
X-RateLimit-Limit-Hour: 2000
X-RateLimit-Remaining-Hour: 1998
```

### Rate Limit Exceeded Response

When rate limit is exceeded, the API returns:

**Status Code:** 429 Too Many Requests

**Headers:**
```
Retry-After: 45
X-RateLimit-Window: minute
```

**Body:**
```json
{
  "error": "Rate limit exceeded",
  "message": "Too many requests. Please retry after 45 seconds.",
  "window": "minute",
  "retryAfter": 45
}
```

## Monitoring

### View Metrics

Access rate limiting metrics via the health endpoint:

```bash
curl http://localhost:5000/api/health/metrics
```

**Response:**
```json
{
  "totalRequests": 1523,
  "totalRateLimitHits": 12,
  "requestsByEndpoint": {
    "quote": 850,
    "historical": 450,
    "fundamentals": 150,
    "profile": 50,
    "search": 23
  },
  "rateLimitHitsByEndpoint": {
    "quote": 10,
    "historical": 2
  },
  "uptime": "02:15:30",
  "rateLimitHitRate": 0.0079
}
```

## Implementation Details

### Endpoint Classification

The middleware classifies requests into categories:

- `/api/stocks/{symbol}/quote` → `quote`
- `/api/stocks/{symbol}/historical` → `historical`
- `/api/stocks/{symbol}/fundamentals` → `fundamentals`
- `/api/stocks/{symbol}/profile` → `profile`
- `/api/stocks/search` → `search`
- Other stock endpoints → `general`

### Token Bucket Keys

Each endpoint has separate buckets for different time windows:

- `quote:minute` - 100 requests per minute
- `quote:hour` - 2000 requests per hour
- `quote:day` - 20000 requests per day

### Thread Safety

All components are thread-safe and use concurrent collections for tracking metrics across multiple requests.

## Testing

To test rate limiting:

```bash
# Send multiple requests quickly
for i in {1..150}; do
  curl http://localhost:5000/api/stocks/AAPL/quote
done
```

After exceeding the limit, you should receive 429 responses with retry-after headers.

## Requirements Satisfied

This implementation satisfies the following requirements:

- **5.1**: Throttles requests and queues them when approaching rate limit
- **5.2**: Returns cached data when rate limit is exceeded (handled by service layer)
- **5.3**: Tracks API usage per endpoint and time window
- **5.5**: Exposes metrics showing current API usage

## Future Enhancements

- Per-user rate limiting (currently global)
- Redis-based distributed rate limiting for multi-instance deployments
- Dynamic rate limit adjustment based on API provider quotas
- Request queuing with priority levels
