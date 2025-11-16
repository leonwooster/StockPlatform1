# Troubleshooting Guide

## Certificate Errors (ERR_CERT_AUTHORITY_INVALID)

If you see certificate errors in the browser console like:
```
Failed to load resource: net::ERR_CERT_AUTHORITY_INVALID
```

This happens when trying to connect to `https://localhost:5565` with a self-signed certificate.

### Solution 1: Use HTTP (Recommended for Development)

The frontend is configured to use HTTP by default in development mode:

```bash
# In frontend/.env.development
VITE_API_BASE_URL=http://localhost:5566/api
```

Make sure your backend is running and accessible at `http://localhost:5566`

### Solution 2: Accept the Self-Signed Certificate

If you want to use HTTPS:

1. Navigate directly to `https://localhost:5565/api/health` in your browser
2. Accept the security warning and add an exception for the certificate
3. Update `frontend/.env.development`:
   ```
   VITE_API_BASE_URL=https://localhost:5565/api
   ```
4. Restart the frontend dev server

## CORS Errors

If you see CORS errors like:
```
Access to fetch at 'http://localhost:5566/api/...' from origin 'http://localhost:5173' has been blocked by CORS policy
```

The backend already has CORS configured to allow all origins in development. Make sure:

1. The backend is running
2. The backend `Program.cs` has the CORS policy configured (it should already be there)
3. Try restarting both frontend and backend

## Backend Not Running

If you see errors like:
```
Failed to fetch: net::ERR_CONNECTION_REFUSED
```

The backend is not running. Start it with:

```bash
cd backend/src/StockSensePro.API
dotnet run
```

Or use Visual Studio / Rider to run the project.

## Port Conflicts

If the backend can't start because ports are in use:

1. Check what's using the ports:
   ```bash
   # Windows
   netstat -ano | findstr :5565
   netstat -ano | findstr :5566
   ```

2. Kill the process or change the ports in `backend/src/StockSensePro.API/Properties/launchSettings.json`

## Frontend Not Loading Data

If the dashboard loads but shows no data:

1. Open browser DevTools (F12)
2. Check the Console tab for errors
3. Check the Network tab to see if API requests are being made
4. Verify the API responses

Common issues:
- Backend not running
- Wrong API URL in `.env.development`
- Rate limiting (check the metrics at bottom of dashboard)
- Yahoo Finance API issues (check `/api/health` endpoint)

## Environment Variables Not Loading

If changes to `.env.development` aren't taking effect:

1. Restart the Vite dev server (Ctrl+C and `npm run dev` again)
2. Clear browser cache
3. Check that the file is named exactly `.env.development` (not `.env.dev`)

## Yahoo Finance API Errors

If you see errors related to Yahoo Finance:

1. Check the health endpoint: `http://localhost:5566/api/health`
2. Check rate limit metrics: `http://localhost:5566/api/health/metrics`
3. Verify your internet connection
4. Yahoo Finance might be temporarily unavailable

## Database Connection Errors

If the backend fails to start with database errors:

1. Make sure PostgreSQL is running
2. Check connection string in `backend/src/StockSensePro.API/appsettings.json`
3. Verify database exists and credentials are correct

## Redis Connection Errors

If you see Redis connection errors:

1. Make sure Redis is running
2. Check connection string in `backend/src/StockSensePro.API/appsettings.json`
3. Default is `localhost` - update if Redis is running elsewhere

## Still Having Issues?

1. Check all services are running:
   - Backend API (http://localhost:5566)
   - PostgreSQL database
   - Redis cache
   - Frontend dev server (http://localhost:5173)

2. Check the logs:
   - Backend console output
   - Browser DevTools console
   - Browser DevTools Network tab

3. Try a clean restart:
   ```bash
   # Stop everything
   # Then start in order:
   
   # 1. Start PostgreSQL and Redis
   
   # 2. Start backend
   cd backend/src/StockSensePro.API
   dotnet run
   
   # 3. Start frontend
   cd frontend
   npm run dev
   ```

## Quick Test

To verify everything is working:

1. Backend health check:
   ```bash
   curl http://localhost:5566/api/health
   ```
   Should return: `{"status":"Healthy",...}`

2. Test a quote endpoint:
   ```bash
   curl http://localhost:5566/api/stocks/AAPL/quote
   ```
   Should return market data for Apple stock

3. Open frontend:
   Navigate to `http://localhost:5173` and check if the dashboard loads with real data
