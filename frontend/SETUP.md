# Frontend Setup Guide

## Prerequisites

- Node.js 18+ installed
- Backend API running (see backend documentation)
- PostgreSQL and Redis running

## Quick Start

1. **Install dependencies**
   ```bash
   cd frontend
   npm install
   ```

2. **Configure environment**
   
   The `.env.development` file is already configured for local development:
   ```bash
   VITE_API_BASE_URL=http://localhost:5566/api
   ```
   
   No changes needed unless your backend runs on a different port.

3. **Start the development server**
   ```bash
   npm run dev
   ```
   
   The frontend will be available at `http://localhost:5173`

4. **Verify it's working**
   
   - Navigate to `http://localhost:5173`
   - You should see the dashboard with real market data
   - Check the browser console for any errors

## Available Scripts

```bash
# Start development server
npm run dev

# Build for production
npm run build

# Preview production build
npm run preview

# Run tests
npm run test
```

## Project Structure

```
frontend/
├── src/
│   ├── assets/          # Static assets
│   ├── components/      # Reusable Vue components
│   │   └── StockSearch.vue
│   ├── composables/     # Vue composables
│   │   └── useMarketData.js
│   ├── router/          # Vue Router configuration
│   ├── services/        # API service layer
│   │   └── api.js
│   ├── store/           # Pinia store
│   ├── views/           # Page components
│   │   ├── DashboardView.vue
│   │   ├── StockDetailView.vue
│   │   ├── AIAgentsView.vue
│   │   ├── TechnicalView.vue
│   │   ├── ScreenerView.vue
│   │   └── NewsView.vue
│   ├── App.vue          # Root component
│   └── main.js          # Application entry point
├── .env.development     # Development environment variables
├── .env.production      # Production environment variables
├── package.json
└── vite.config.js
```

## Key Features

### Yahoo Finance Integration

The frontend now integrates with Yahoo Finance APIs:

- **Real-time quotes** - Live stock prices and market data
- **Historical data** - Price history with multiple time intervals
- **Fundamentals** - Financial metrics and ratios
- **Company profiles** - Company information and details
- **Stock search** - Search by symbol or company name

### Components

**StockSearch.vue**
- Autocomplete search component
- Debounced API calls
- Clean, modern UI

**DashboardView.vue**
- Live market indices (S&P 500, NASDAQ 100)
- Top gainers and losers
- Watchlist with real-time prices
- Rate limit metrics
- API health status
- Auto-refresh every 60 seconds

**StockDetailView.vue**
- Comprehensive stock information
- Real-time quotes
- Company profile
- Financial metrics

### Composables

**useMarketData.js**
- Reactive state management for market data
- Helper functions for formatting
- Batch quote fetching
- Loading and error states

### Services

**api.js**
- Centralized API client
- Rate limit error handling
- Type-safe methods for all endpoints

## Environment Variables

### Development

Create or edit `.env.development`:

```bash
# Backend API URL (HTTP for local development)
VITE_API_BASE_URL=http://localhost:5566/api
```

### Production

Create or edit `.env.production`:

```bash
# Production API URL
VITE_API_BASE_URL=https://api.yourdomain.com/api
```

## Common Issues

### Certificate Errors

If you see `ERR_CERT_AUTHORITY_INVALID` errors:
- Use HTTP instead of HTTPS for local development
- The default configuration already uses HTTP
- See [TROUBLESHOOTING.md](./TROUBLESHOOTING.md) for more details

### CORS Errors

If you see CORS errors:
- Make sure the backend is running
- The backend should have CORS configured to allow all origins in development
- Restart both frontend and backend

### No Data Loading

If the dashboard shows no data:
1. Check browser console for errors
2. Verify backend is running at `http://localhost:5566`
3. Test the health endpoint: `http://localhost:5566/api/health`
4. Check rate limit metrics at bottom of dashboard

## Testing the Integration

### 1. Test Backend Connection

```bash
# Test health endpoint
curl http://localhost:5566/api/health

# Test quote endpoint
curl http://localhost:5566/api/stocks/AAPL/quote

# Test search endpoint
curl "http://localhost:5566/api/stocks/search?query=Apple&limit=5"
```

### 2. Test Frontend

1. Open `http://localhost:5173` in your browser
2. Open DevTools (F12) and check the Console tab
3. You should see market data loading on the dashboard
4. Try the stock search functionality
5. Check the rate limit metrics at the bottom

### 3. Verify Real-Time Updates

1. Watch the dashboard for 60 seconds
2. Market data should auto-refresh
3. Check the Network tab in DevTools to see API calls

## Next Steps

1. **Explore the Dashboard** - See real-time market data
2. **Try Stock Search** - Search for different stocks
3. **Check Rate Limits** - Monitor API usage at bottom of dashboard
4. **View Stock Details** - Click on stocks to see detailed information
5. **Customize** - Modify watchlist symbols in `DashboardView.vue`

## Production Deployment

### Build for Production

```bash
npm run build
```

This creates an optimized build in the `dist/` directory.

### Environment Configuration

Update `.env.production` with your production API URL:

```bash
VITE_API_BASE_URL=https://api.yourdomain.com/api
```

### Deploy

Deploy the `dist/` directory to your hosting provider:
- Netlify
- Vercel
- AWS S3 + CloudFront
- Azure Static Web Apps
- Any static hosting service

### Important Notes

- Make sure your production backend has proper CORS configuration
- Use HTTPS in production
- Configure proper rate limiting
- Set up monitoring and error tracking

## Support

For issues and troubleshooting:
- See [TROUBLESHOOTING.md](./TROUBLESHOOTING.md)
- Check [YAHOO_FINANCE_INTEGRATION.md](./YAHOO_FINANCE_INTEGRATION.md) for API documentation
- Review browser console and network tab for errors
