# Yahoo Finance Frontend Integration

## Overview

The frontend has been updated to integrate with the Yahoo Finance backend APIs. This document describes the new features and how to use them.

## New Features

### 1. Market Data Service (`api.js`)

Added new methods to fetch Yahoo Finance data:

```javascript
// Get real-time quote for a symbol
await stockService.getQuote('AAPL')

// Get historical prices
await stockService.getHistoricalPrices('AAPL', {
  startDate: '2024-01-01',
  endDate: '2024-12-31',
  interval: 'Daily' // Daily, Weekly, or Monthly
})

// Get fundamental data
await stockService.getFundamentals('AAPL')

// Get company profile
await stockService.getCompanyProfile('AAPL')

// Search for stocks
await stockService.searchSymbols('Apple', 10)

// Get multiple quotes at once
await stockService.getQuotes(['AAPL', 'MSFT', 'GOOGL'])
```

### 2. Market Data Composable (`useMarketData.js`)

A Vue composable for managing market data state:

```javascript
import { useMarketData } from '@/composables/useMarketData'

const { 
  quotes,           // Reactive quotes object
  loading,          // Loading state
  error,            // Error state
  fetchQuote,       // Fetch single quote
  fetchMultipleQuotes, // Fetch multiple quotes
  getQuote,         // Get computed quote
  formatPrice,      // Format price as currency
  formatPercent,    // Format percentage
  formatVolume,     // Format volume (K, M, B)
  formatMarketCap   // Format market cap (M, B, T)
} = useMarketData()
```

### 3. Stock Search Component (`StockSearch.vue`)

A reusable search component with autocomplete:

```vue
<template>
  <StockSearch @select="handleStockSelect" />
</template>

<script setup>
const handleStockSelect = (stock) => {
  console.log('Selected:', stock.symbol, stock.name)
}
</script>
```

### 4. Updated Dashboard View

The dashboard now displays real market data:

- **Live Market Indices**: S&P 500 and NASDAQ 100 with real-time prices
- **Top Gainers/Losers**: Dynamically calculated from tracked stocks
- **Watchlist**: Real-time prices for watchlist symbols
- **Rate Limit Metrics**: Monitor API usage and rate limiting
- **Health Status**: Display API connectivity status
- **Auto-refresh**: Market data refreshes every 60 seconds

### 5. Stock Detail View (New)

A dedicated view for detailed stock information:

- Real-time quote with price, change, and volume
- Company profile (sector, industry, employees, website, description)
- Key financial metrics (P/E ratio, EPS, dividend yield, beta, 52-week range)
- Stock search with autocomplete

## Rate Limiting

The frontend handles rate limiting gracefully:

- **429 Responses**: Displays user-friendly error messages
- **Retry-After Headers**: Respects backend rate limit guidance
- **Error Notifications**: Shows alerts when rate limits are exceeded

Example error handling:

```javascript
try {
  const quote = await stockService.getQuote('AAPL')
} catch (error) {
  if (error.message.includes('Rate limit exceeded')) {
    // Show user-friendly message
    console.log('Please wait before making more requests')
  }
}
```

## Configuration

The API base URL is configured using environment variables:

**Development** (`.env.development`):
```bash
VITE_API_BASE_URL=http://localhost:5566/api
```

**Production** (`.env.production`):
```bash
VITE_API_BASE_URL=https://api.yourdomain.com/api
```

The frontend will automatically use the correct URL based on the environment.

### Backend Ports

The backend runs on:
- HTTP: `http://localhost:5566` (recommended for development)
- HTTPS: `https://localhost:5565` (requires accepting self-signed certificate)

### Troubleshooting

If you encounter certificate errors or connection issues, see [TROUBLESHOOTING.md](./TROUBLESHOOTING.md) for solutions.

## Usage Examples

### Fetching Market Data in a Component

```vue
<script setup>
import { ref, onMounted } from 'vue'
import { useMarketData } from '@/composables/useMarketData'

const { fetchQuote, getQuote, formatPrice, formatPercent } = useMarketData()
const symbol = ref('AAPL')
const quote = getQuote(symbol.value)

onMounted(async () => {
  await fetchQuote(symbol.value)
})
</script>

<template>
  <div v-if="quote">
    <h2>{{ quote.symbol }}</h2>
    <p>{{ formatPrice(quote.currentPrice) }}</p>
    <p>{{ formatPercent(quote.changePercent) }}</p>
  </div>
</template>
```

### Searching for Stocks

```vue
<script setup>
import { ref } from 'vue'
import { stockService } from '@/services/api'

const searchQuery = ref('')
const results = ref([])

const search = async () => {
  if (searchQuery.value.length < 2) return
  results.value = await stockService.searchSymbols(searchQuery.value)
}
</script>

<template>
  <input v-model="searchQuery" @input="search" placeholder="Search stocks..." />
  <ul>
    <li v-for="stock in results" :key="stock.symbol">
      {{ stock.symbol }} - {{ stock.name }}
    </li>
  </ul>
</template>
```

### Fetching Historical Data

```vue
<script setup>
import { ref, onMounted } from 'vue'
import { stockService } from '@/services/api'

const historicalData = ref([])

onMounted(async () => {
  const endDate = new Date().toISOString().split('T')[0]
  const startDate = new Date(Date.now() - 90 * 24 * 60 * 60 * 1000)
    .toISOString().split('T')[0]
  
  historicalData.value = await stockService.getHistoricalPrices('AAPL', {
    startDate,
    endDate,
    interval: 'Daily'
  })
})
</script>

<template>
  <div v-for="price in historicalData" :key="price.date">
    {{ price.date }}: {{ price.close }}
  </div>
</template>
```

## Health Monitoring

Check API health and rate limit metrics:

```javascript
import { healthService } from '@/services/api'

// Check API health
const health = await healthService.getHealth()
console.log('Status:', health.status)
console.log('Details:', health.details)

// Get rate limit metrics
const metrics = await healthService.getMetrics()
console.log('Total Requests:', metrics.totalRequests)
console.log('Rate Limit Hits:', metrics.totalRateLimitHits)
console.log('Hit Rate:', metrics.rateLimitHitRate)
```

## Error Handling

All API methods include proper error handling:

```javascript
try {
  const quote = await stockService.getQuote('INVALID')
} catch (error) {
  if (error.message.includes('Rate limit exceeded')) {
    // Handle rate limiting
  } else if (error.message.includes('Failed to fetch')) {
    // Handle network errors
  } else {
    // Handle other errors
  }
}
```

## Testing

To test the integration:

1. Start the backend API
2. Start the frontend dev server: `npm run dev`
3. Navigate to the Dashboard view
4. Verify that market data loads
5. Try searching for stocks
6. Check the rate limit metrics at the bottom

## Future Enhancements

- Add charting for historical price data
- Implement real-time WebSocket updates
- Add portfolio tracking with Yahoo Finance data
- Create technical analysis indicators
- Add news feed integration
- Implement advanced filtering and screening
