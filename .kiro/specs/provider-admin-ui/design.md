# Design Document - Provider Admin UI

## Overview

The Provider Admin UI is a React-based administrative dashboard that provides real-time monitoring and configuration capabilities for the multi-provider stock data system. The interface will be built as a new section within the existing StockSensePro frontend application.

## Architecture

### High-Level Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    React Frontend                            │
│  ┌────────────────────────────────────────────────────────┐ │
│  │         Provider Admin Dashboard (New)                 │ │
│  │  - Provider Status View                                │ │
│  │  - Metrics Dashboard                                   │ │
│  │  - Configuration Panel                                 │ │
│  │  - Cost Tracking View                                  │ │
│  └────────────────────────────────────────────────────────┘ │
│  ┌────────────────────────────────────────────────────────┐ │
│  │         Existing Stock Trading UI                      │ │
│  └────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
                           │
                           │ REST API
                           ▼
┌─────────────────────────────────────────────────────────────┐
│                    Backend API                               │
│  ┌────────────────────────────────────────────────────────┐ │
│  │         Health Controller (Existing)                   │ │
│  │  - GET /api/health                                     │ │
│  │  - GET /api/health/metrics                             │ │
│  └────────────────────────────────────────────────────────┘ │
│  ┌────────────────────────────────────────────────────────┐ │
│  │         Provider Admin Controller (New)                │ │
│  │  - GET /api/admin/providers                            │ │
│  │  - PUT /api/admin/providers/strategy                   │ │
│  │  - PUT /api/admin/providers/config                     │ │
│  │  - POST /api/admin/providers/test                      │ │
│  └────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
```

## Components and Interfaces

### Frontend Components

#### 1. ProviderAdminDashboard (Container)

**Purpose**: Main container component that orchestrates the admin dashboard

**Props**: None (uses React Router for navigation)

**State**:
```typescript
interface DashboardState {
  providers: ProviderStatus[];
  metrics: ProviderMetrics;
  config: ProviderConfig;
  loading: boolean;
  error: string | null;
  lastUpdate: Date;
}
```

**Children**:
- `<ProviderStatusPanel />`
- `<MetricsPanel />`
- `<RateLimitPanel />`
- `<CostTrackingPanel />`
- `<ConfigurationPanel />`

#### 2. ProviderStatusPanel

**Purpose**: Displays real-time health status of all providers

**Props**:
```typescript
interface ProviderStatusPanelProps {
  providers: ProviderStatus[];
  onRefresh: () => void;
  lastUpdate: Date;
}
```

**UI Elements**:
- Provider cards with status indicators
- Health check timestamp
- Average response time
- Consecutive failures count
- Manual refresh button

#### 3. MetricsPanel

**Purpose**: Visualizes provider performance metrics

**Props**:
```typescript
interface MetricsPanelProps {
  metrics: ProviderMetrics;
  selectedProvider: string;
  onProviderSelect: (provider: string) => void;
  timeRange: TimeRange;
  onTimeRangeChange: (range: TimeRange) => void;
}
```

**UI Elements**:
- Provider selector dropdown
- Time range selector (1h, 24h, 7d, 30d)
- Request count statistics
- Error rate gauge
- Response time chart (line chart)
- Request volume chart (bar chart)

#### 4. RateLimitPanel

**Purpose**: Monitors rate limit usage across providers

**Props**:
```typescript
interface RateLimitPanelProps {
  rateLimits: RateLimitStatus[];
  onProviderSelect: (provider: string) => void;
}
```

**UI Elements**:
- Rate limit progress bars (minute/daily)
- Usage percentage indicators
- Time until reset countdown
- Warning indicators (80%, 90% thresholds)
- Historical usage chart

#### 5. CostTrackingPanel

**Purpose**: Tracks and visualizes API usage costs

**Props**:
```typescript
interface CostTrackingPanelProps {
  costs: CostData;
  budget: BudgetConfig;
  onExport: () => void;
}
```

**UI Elements**:
- Cost summary cards (today, month, total)
- Cost breakdown by provider (pie chart)
- Daily cost trend (line chart)
- Budget progress bar
- Cost per request metrics
- Export button

#### 6. ConfigurationPanel

**Purpose**: Allows administrators to configure provider settings

**Props**:
```typescript
interface ConfigurationPanelProps {
  config: ProviderConfig;
  onSave: (config: ProviderConfig) => Promise<void>;
  onTest: (provider: string, symbol: string) => Promise<TestResult>;
}
```

**UI Elements**:
- Strategy selector (radio buttons)
- Primary provider dropdown
- Fallback provider dropdown
- Enable/disable toggles per provider
- Test provider tool
- Save/Cancel buttons
- Confirmation modal

#### 7. ProviderTestTool

**Purpose**: Manual testing interface for individual providers

**Props**:
```typescript
interface ProviderTestToolProps {
  providers: string[];
  onTest: (provider: string, symbol: string) => Promise<TestResult>;
}
```

**UI Elements**:
- Provider selector
- Symbol input field
- Test button
- Results display (JSON viewer)
- Response time indicator
- Error display

### Data Models

#### ProviderStatus

```typescript
interface ProviderStatus {
  name: string;
  type: 'YahooFinance' | 'AlphaVantage' | 'Mock';
  isHealthy: boolean;
  lastChecked: Date;
  consecutiveFailures: number;
  averageResponseTime: number;
  lastError?: string;
}
```

#### ProviderMetrics

```typescript
interface ProviderMetrics {
  provider: string;
  totalRequests: number;
  successfulRequests: number;
  failedRequests: number;
  errorRate: number;
  responseTime: {
    p50: number;
    p95: number;
    p99: number;
    average: number;
  };
  requestHistory: TimeSeriesData[];
  responseTimeHistory: TimeSeriesData[];
}
```

#### RateLimitStatus

```typescript
interface RateLimitStatus {
  provider: string;
  minute: {
    limit: number;
    remaining: number;
    resetAt: Date;
  };
  daily: {
    limit: number;
    remaining: number;
    resetAt: Date;
  };
}
```

#### CostData

```typescript
interface CostData {
  providers: {
    [key: string]: {
      requestsToday: number;
      requestsThisMonth: number;
      costToday: number;
      costThisMonth: number;
      costPerRequest: number;
    };
  };
  total: {
    costToday: number;
    costThisMonth: number;
  };
  history: DailyCost[];
}

interface DailyCost {
  date: Date;
  cost: number;
  requests: number;
}
```

#### ProviderConfig

```typescript
interface ProviderConfig {
  strategy: 'Primary' | 'Fallback' | 'RoundRobin' | 'CostOptimized';
  primaryProvider: string;
  fallbackProvider?: string;
  enableAutomaticFallback: boolean;
  healthCheckInterval: number;
  providers: {
    [key: string]: {
      enabled: boolean;
      rateLimit?: {
        requestsPerMinute: number;
        requestsPerDay: number;
      };
    };
  };
}
```

### API Endpoints (New)

#### GET /api/admin/providers

**Purpose**: Get all provider statuses and configuration

**Response**:
```json
{
  "providers": [
    {
      "name": "AlphaVantage",
      "type": "AlphaVantage",
      "isHealthy": true,
      "lastChecked": "2025-11-22T15:30:00Z",
      "consecutiveFailures": 0,
      "averageResponseTime": 250,
      "enabled": true
    }
  ],
  "config": {
    "strategy": "Fallback",
    "primaryProvider": "AlphaVantage",
    "fallbackProvider": "YahooFinance",
    "enableAutomaticFallback": true
  }
}
```

#### GET /api/admin/providers/metrics

**Purpose**: Get detailed metrics for all providers

**Query Parameters**:
- `provider` (optional): Filter by specific provider
- `timeRange` (optional): 1h, 24h, 7d, 30d (default: 24h)

**Response**:
```json
{
  "provider": "AlphaVantage",
  "timeRange": "24h",
  "totalRequests": 450,
  "successfulRequests": 445,
  "failedRequests": 5,
  "errorRate": 0.011,
  "responseTime": {
    "p50": 180,
    "p95": 350,
    "p99": 500,
    "average": 220
  },
  "requestHistory": [
    { "timestamp": "2025-11-22T14:00:00Z", "count": 25 }
  ]
}
```

#### GET /api/admin/providers/rate-limits

**Purpose**: Get current rate limit status for all providers

**Response**:
```json
{
  "providers": {
    "AlphaVantage": {
      "minute": {
        "limit": 5,
        "remaining": 3,
        "resetAt": "2025-11-22T15:31:00Z"
      },
      "daily": {
        "limit": 25,
        "remaining": 18,
        "resetAt": "2025-11-23T00:00:00Z"
      }
    }
  }
}
```

#### GET /api/admin/providers/costs

**Purpose**: Get cost tracking data

**Query Parameters**:
- `period` (optional): today, month, all (default: month)

**Response**:
```json
{
  "providers": {
    "AlphaVantage": {
      "requestsToday": 450,
      "requestsThisMonth": 8500,
      "costToday": 0.90,
      "costThisMonth": 17.00,
      "costPerRequest": 0.002
    }
  },
  "total": {
    "costToday": 0.90,
    "costThisMonth": 17.00
  },
  "history": [
    { "date": "2025-11-21", "cost": 0.85, "requests": 425 }
  ]
}
```

#### PUT /api/admin/providers/strategy

**Purpose**: Update provider selection strategy

**Request Body**:
```json
{
  "strategy": "Fallback",
  "primaryProvider": "AlphaVantage",
  "fallbackProvider": "YahooFinance",
  "enableAutomaticFallback": true
}
```

**Response**:
```json
{
  "success": true,
  "message": "Strategy updated successfully",
  "appliedAt": "2025-11-22T15:30:00Z"
}
```

#### PUT /api/admin/providers/config

**Purpose**: Update provider configuration

**Request Body**:
```json
{
  "provider": "AlphaVantage",
  "enabled": true,
  "rateLimit": {
    "requestsPerMinute": 75,
    "requestsPerDay": 500
  }
}
```

**Response**:
```json
{
  "success": true,
  "message": "Provider configuration updated",
  "requiresRestart": false
}
```

#### POST /api/admin/providers/test

**Purpose**: Test a specific provider manually

**Request Body**:
```json
{
  "provider": "AlphaVantage",
  "symbol": "AAPL"
}
```

**Response**:
```json
{
  "success": true,
  "provider": "AlphaVantage",
  "symbol": "AAPL",
  "responseTime": 245,
  "data": {
    "symbol": "AAPL",
    "currentPrice": 271.49,
    "change": 5.24
  }
}
```

## UI/UX Design

### Layout Structure

```
┌─────────────────────────────────────────────────────────────┐
│  Header: Provider Admin Dashboard                    [User] │
├─────────────────────────────────────────────────────────────┤
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐        │
│  │  Overview   │  │   Metrics   │  │    Config   │        │
│  └─────────────┘  └─────────────┘  └─────────────┘        │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  ┌──────────────────────────────────────────────────────┐  │
│  │  Provider Status Cards                                │  │
│  │  ┌──────────┐  ┌──────────┐  ┌──────────┐           │  │
│  │  │ Alpha    │  │ Yahoo    │  │  Mock    │           │  │
│  │  │ Vantage  │  │ Finance  │  │          │           │  │
│  │  │ ● Healthy│  │ ● Healthy│  │ ● Healthy│           │  │
│  │  │ 250ms    │  │ 150ms    │  │ 10ms     │           │  │
│  │  └──────────┘  └──────────┘  └──────────┘           │  │
│  └──────────────────────────────────────────────────────┘  │
│                                                              │
│  ┌──────────────────────────────────────────────────────┐  │
│  │  Rate Limits                                          │  │
│  │  Alpha Vantage: [████████░░] 80% (4/5 per min)      │  │
│  │  Daily: [███████░░░] 72% (18/25 per day)            │  │
│  └──────────────────────────────────────────────────────┘  │
│                                                              │
│  ┌──────────────────────────────────────────────────────┐  │
│  │  Metrics & Charts                                     │  │
│  │  [Request Volume Chart]  [Response Time Chart]       │  │
│  └──────────────────────────────────────────────────────┘  │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

### Color Scheme

- **Healthy Status**: Green (#10B981)
- **Unhealthy Status**: Red (#EF4444)
- **Warning (80%)**: Yellow (#F59E0B)
- **Critical (90%)**: Orange (#F97316)
- **Primary Action**: Blue (#3B82F6)
- **Background**: Light Gray (#F9FAFB)
- **Cards**: White (#FFFFFF)

### Typography

- **Headers**: Inter, 24px, Bold
- **Subheaders**: Inter, 18px, Semibold
- **Body**: Inter, 14px, Regular
- **Metrics**: Roboto Mono, 16px, Medium

## Data Flow

### Real-Time Updates

```
Component Mount
    │
    ├─> Initial Data Fetch
    │   └─> GET /api/admin/providers
    │   └─> GET /api/admin/providers/metrics
    │   └─> GET /api/admin/providers/rate-limits
    │
    ├─> Start Polling (30s interval)
    │   └─> Update provider status
    │   └─> Update rate limits
    │
    └─> Start Polling (60s interval)
        └─> Update metrics
        └─> Update costs
```

### Configuration Update Flow

```
User Changes Config
    │
    ├─> Validate Input
    │   └─> Check required fields
    │   └─> Validate provider selection
    │
    ├─> Show Confirmation Modal
    │   └─> Display changes summary
    │   └─> Warn about impacts
    │
    ├─> User Confirms
    │   └─> PUT /api/admin/providers/strategy
    │   └─> Show loading spinner
    │
    └─> Handle Response
        ├─> Success: Show success toast
        │   └─> Refresh data
        │   └─> Update UI
        │
        └─> Error: Show error message
            └─> Revert UI changes
```

## Error Handling

### Error States

1. **Network Error**: Display retry button and error message
2. **Authentication Error**: Redirect to login
3. **Authorization Error**: Display access denied message
4. **Validation Error**: Highlight invalid fields with error messages
5. **Server Error**: Display generic error with support contact

### Error Display

```typescript
interface ErrorState {
  type: 'network' | 'auth' | 'validation' | 'server';
  message: string;
  details?: string;
  retryable: boolean;
}
```

## Testing Strategy

### Unit Tests

- Component rendering tests
- State management tests
- Data transformation tests
- Utility function tests

### Integration Tests

- API integration tests
- Component interaction tests
- Navigation tests
- Form submission tests

### E2E Tests

- Complete user workflows
- Configuration changes
- Provider testing
- Data export

## Performance Considerations

### Optimization Strategies

1. **Lazy Loading**: Load dashboard components on demand
2. **Memoization**: Cache expensive calculations
3. **Virtual Scrolling**: For large metric lists
4. **Debouncing**: For search and filter inputs
5. **Code Splitting**: Separate bundle for admin dashboard

### Performance Targets

- Initial load: < 2 seconds
- Time to interactive: < 3 seconds
- API response time: < 500ms
- Chart rendering: < 100ms
- Real-time update latency: < 1 second

## Security Considerations

### Authentication

- Require JWT token for all admin endpoints
- Validate token on every request
- Implement token refresh mechanism

### Authorization

- Check for "Admin" role in JWT claims
- Implement role-based access control (RBAC)
- Log all configuration changes with user identity

### Data Protection

- Sanitize all user inputs
- Validate all API responses
- Implement CSRF protection
- Use HTTPS for all communications

## Accessibility

### WCAG 2.1 AA Compliance

- Keyboard navigation support
- Screen reader compatibility
- Sufficient color contrast (4.5:1 minimum)
- Focus indicators on interactive elements
- ARIA labels for all controls
- Alt text for all images and icons

## Browser Support

- Chrome 90+
- Firefox 88+
- Safari 14+
- Edge 90+

## Dependencies

### Frontend Libraries

- React 18+
- TypeScript 5+
- React Router 6+
- Recharts (for charts)
- Tailwind CSS (for styling)
- React Query (for data fetching)
- Zustand (for state management)
- date-fns (for date formatting)

### Backend Requirements

- New ProviderAdminController
- Authorization middleware
- Audit logging service

